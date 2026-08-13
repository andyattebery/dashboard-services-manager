{
  lib,
  stdenv,
  fetchurl,
  patchelf,
  makeWrapper,
  icu,
  openssl,
  writeShellScript,
  common-updater-scripts,
}:

let
  version = "1.2.7";

  # One fetcher per published release asset. Three things depend on this attrset and nothing
  # else, so adding an architecture here is the only edit needed anywhere:
  #
  #   * `src` selects this system's entry, and an unlisted system gets an explicit throw
  #     rather than silently being handed the x64 tarball.
  #   * `meta.platforms` is `builtins.attrNames sources`.
  #   * `passthru.updateScript` iterates those platforms, so the updater extends itself.
  #
  # These must be `fetchurl` DERIVATIONS, not inert { url; hash; } records. Each is built from
  # the local `pkgs`, so every platform's fetcher is buildable on any machine -- which is what
  # lets `update-source-version --source-key=sources.<platform>` refresh the darwin hash from
  # an x86_64 runner. A fixed-output hash depends only on the bytes fetched, never on who
  # fetched them. Store plain data here instead and that property is lost.
  #
  # Formatting is NOT load-bearing: update-source-version matches on the old hash's value and
  # refuses to act if it is not unique in the file. An earlier revision rewrote these with
  # line-shaped seds, which a formatter could silently break.
  sources = {
    x86_64-linux = fetchurl {
      url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-linux-x64.tar.gz";
      hash = "sha256-IHClR4GrHdT90v7o0Baf9RpwQV8atm7hvm2tPzsLfpY=";
    };
    aarch64-linux = fetchurl {
      url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-linux-arm64.tar.gz";
      hash = "sha256-aRccEoIxo2izXrVpaP2fnuMU5BW/6HJymWc6OlMhz4M=";
    };
    aarch64-darwin = fetchurl {
      url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-macos-arm64.tar.gz";
      hash = "sha256-ZLUimNixd+oPbeo9ZZmce8FZ+0RzEzNkZDk6RI0nBwU=";
    };
  };

  # Linux only. The macOS binary links nothing outside /System and /usr/lib -- Foundation,
  # Security, CryptoKit, libSystem, the Swift runtime -- so there is no darwin equivalent of
  # this list and no wrapper on that platform. (If one is ever needed, the variable on darwin
  # is DYLD_FALLBACK_LIBRARY_PATH, not LD_LIBRARY_PATH.)
  runtimeLibs = [
    icu
    openssl
    stdenv.cc.cc.lib
  ];
in
# `version` is bound above rather than read from `finalAttrs` because `sources` needs it, and
# `sources` is evaluated outside this lambda where `finalAttrs` does not exist.
stdenv.mkDerivation (finalAttrs: {
  pname = "dsm-provider";
  inherit version;

  src =
    sources.${stdenv.hostPlatform.system}
      or (throw "dsm-provider: no published binary for ${stdenv.hostPlatform.system}");

  # Both tarballs extract their files directly, with no wrapping directory.
  sourceRoot = ".";

  nativeBuildInputs = lib.optionals stdenv.hostPlatform.isLinux [
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
  #
  # None of that applies to macOS: Mach-O has no ELF interpreter to set, the binary is already
  # ad-hoc signed, and modifying it would invalidate that signature. So darwin just installs
  # it. That is why the phases below branch rather than sharing a common install.
  dontPatchELF = true;
  dontStrip = true;

  installPhase =
    ''
      runHook preInstall
    ''
    + lib.optionalString stdenv.hostPlatform.isLinux ''
      install -Dm755 Dsm.Provider.App $out/bin/.dsm-provider-unwrapped
      patchelf --set-interpreter "$(cat $NIX_CC/nix-support/dynamic-linker)" $out/bin/.dsm-provider-unwrapped
      makeWrapper $out/bin/.dsm-provider-unwrapped $out/bin/dsm-provider \
        --prefix LD_LIBRARY_PATH : "${lib.makeLibraryPath runtimeLibs}"
    ''
    + lib.optionalString stdenv.hostPlatform.isDarwin ''
      install -Dm755 Dsm.Provider.App $out/bin/dsm-provider
    ''
    + ''
      runHook postInstall
    '';

  # Run the binary at build time. This is a minority choice -- most prebuilt packages in
  # nixpkgs never execute what they ship, and the closest analogue (powershell) uses a
  # non-gating `passthru.tests` instead. It is deliberate here: the autoPatchelfHook
  # experiment above produced a green build of a binary that could not start, and a
  # non-gating test would have reported that after the fact and shipped it anyway.
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

  passthru = {
    # Exposed so `update-source-version --source-key="sources.<platform>"` can reach each
    # fetcher as an attribute of the derivation. A let-binding alone is invisible to it, so
    # this is required rather than informational.
    inherit sources;

    # Takes the version as an argument instead of discovering it. nixpkgs' updaters poll
    # upstreams they do not control; this repo publishes its own releases, and the tag is
    # handed to .github/workflows/update-flake.yml as a workflow_dispatch input. Do not
    # "fix" this into a GitHub API poller.
    updateScript = writeShellScript "update-dsm-provider" ''
      set -o errexit
      export PATH="${lib.makeBinPath [ common-updater-scripts ]}:$PATH"

      NEW_VERSION="''${1:?usage: update-dsm-provider <version>}"

      # One call per platform. Each evaluates that platform's fetcher to find the hash it is
      # replacing, so no cross-architecture builder is involved. --ignore-same-version is
      # required because only the first call changes `version`; the rest would otherwise
      # refuse to run against an already-updated file.
      for platform in ${lib.escapeShellArgs (builtins.attrNames sources)}; do
        update-source-version dsm-provider "$NEW_VERSION" \
          --ignore-same-version \
          --source-key="sources.$platform"
      done
    '';
  };

  meta = {
    description = "Provider that discovers running services and posts them to a Dashboard Services Manager API";
    homepage = "https://github.com/andyattebery/dashboard-services-manager";
    license = lib.licenses.mit;
    # A .NET single-file bundle is IL plus a native host, so both apply.
    sourceProvenance = with lib.sourceTypes; [
      binaryBytecode
      binaryNativeCode
    ];
    mainProgram = "dsm-provider";
    platforms = builtins.attrNames sources;
  };
})
