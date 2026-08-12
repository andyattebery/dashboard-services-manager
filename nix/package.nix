{
  lib,
  stdenv,
  fetchurl,
  patchelf,
  makeWrapper,
  icu,
  openssl,
}:

let
  version = "1.2.6";

  # One entry per published release asset. `meta.platforms` is derived from these keys, so
  # adding an architecture here is the only edit needed -- there is no second list to keep
  # in sync, and an unlisted system gets an explicit error rather than silently being handed
  # the x64 tarball (which is what the old `arch = if ... then "arm64" else "x64"` did, and
  # would have fetched a Linux binary on darwin).
  #
  # THE FORMATTING HERE IS LOAD-BEARING. .github/workflows/update-flake.yml rewrites these
  # values with sed, keyed on the system tuple. Keep each `hash = ` on its own line directly
  # under its system key, and keep the arch suffix in the URL. Reflowing this attrset --
  # merging the lines, or running a formatter over it -- makes those seds match nothing, and
  # a sed that matches nothing fails SILENTLY: version and hashes stay mutually consistent,
  # the build passes, and the stale release ships. See that workflow's comments.
  sources = {
    x86_64-linux = {
      url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-linux-x64.tar.gz";
      hash = "sha256-S/KFdB34IulPx0s9zJ8Bju2NOun3nvk8HTVXwuUeZKk=";
    };
    aarch64-linux = {
      url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-linux-arm64.tar.gz";
      hash = "sha256-bgkh4PTbZf5rKQ8/3330Gs6HWQNdC8Tb7lsZgqtfT5o=";
    };
  };

  source =
    sources.${stdenv.hostPlatform.system}
      or (throw "dsm-provider: no published binary for ${stdenv.hostPlatform.system}");

  runtimeLibs = [
    icu
    openssl
    stdenv.cc.cc.lib
  ];
in
# `version` is bound above rather than read from `finalAttrs`, because `sources` needs it and
# `sources` is evaluated outside this lambda, where `finalAttrs` does not exist. libfrida-core
# -- the closest analogue in nixpkgs, also a per-arch prebuilt tarball -- does exactly this.
stdenv.mkDerivation (finalAttrs: {
  pname = "dsm-provider";
  inherit version;

  src = fetchurl { inherit (source) url hash; };

  sourceRoot = ".";

  nativeBuildInputs = [
    patchelf
    makeWrapper
  ];

  # The published binary is a .NET single-file bundle, produced with
  #   --self-contained /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true
  # (see .github/workflows/ci.yaml, build-binaries). Its native libraries are appended to the
  # executable and found by byte offset, so any rewrite of the ELF layout invalidates them.
  #
  # Do NOT replace this with autoPatchelfHook. That was tried. The build still SUCCEEDS, and
  # the resulting binary dies at startup with:
  #     Failure processing application bundle; possible file corruption.
  #     Arithmetic overflow while reading bundle.
  # So `dontPatchELF` stops nixpkgs' automatic patching, the bare --set-interpreter below is
  # the smallest edit that lets it run on NixOS, and the runtime libraries are supplied via
  # LD_LIBRARY_PATH in a wrapper -- never an RPATH -- so the file is not rewritten again.
  dontPatchELF = true;
  dontStrip = true;

  installPhase = ''
    runHook preInstall
    install -Dm755 Dsm.Provider.App $out/bin/.dsm-provider-unwrapped
    patchelf --set-interpreter "$(cat $NIX_CC/nix-support/dynamic-linker)" $out/bin/.dsm-provider-unwrapped
    makeWrapper $out/bin/.dsm-provider-unwrapped $out/bin/dsm-provider \
      --prefix LD_LIBRARY_PATH : "${lib.makeLibraryPath runtimeLibs}"
    runHook postInstall
  '';

  # Run the binary at build time. Most prebuilt packages in nixpkgs never execute what they
  # ship; this one must, because the autoPatchelfHook experiment above produced a green build
  # of a binary that could not start. A non-gating `passthru.tests` would have reported that
  # after the fact and shipped it anyway.
  #
  # Assert on OUTPUT, not exit status. dsm-provider exits 0 even when hosting fails, and a
  # corrupted bundle aborts on SIGABRT having written nothing -- so the exit code is useless
  # in both directions. Only a message that proves application code ran separates the cases.
  #
  # There is deliberately no negative grep for the bundle-corruption text: when that failure
  # was reproduced the log came back EMPTY, so such a grep would not have fired. It would
  # guard one spelling of one failure, while the positive assertion below catches every
  # failure mode including that one.
  doInstallCheck = true;
  installCheckPhase = ''
    runHook preInstallCheck
    timeout 120 $out/bin/dsm-provider > check.log 2>&1 || true
    grep -q 'ApiUrl field is required' check.log \
      || { echo "dsm-provider did not reach application code:"; cat check.log; exit 1; }
    runHook postInstallCheck
  '';

  meta = {
    description = "Provider that discovers running services and posts them to a Dashboard Services Manager API";
    homepage = "https://github.com/andyattebery/dashboard-services-manager";
    license = lib.licenses.mit;
    sourceProvenance = with lib.sourceTypes; [ binaryNativeCode ];
    mainProgram = "dsm-provider";
    platforms = builtins.attrNames sources;
  };
})
