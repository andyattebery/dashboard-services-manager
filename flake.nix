{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.11";

  outputs = { self, nixpkgs }:
  let
    forAllSystems = nixpkgs.lib.genAttrs [ "x86_64-linux" "aarch64-linux" ];
  in {
    packages = forAllSystems (system:
    let
      pkgs = nixpkgs.legacyPackages.${system};
      arch = if system == "aarch64-linux" then "arm64" else "x64";
      runtimeLibs = with pkgs; [ icu openssl stdenv.cc.cc.lib ];
    in {
      dsm-provider = pkgs.stdenv.mkDerivation rec {
        pname = "dsm-provider";
        version = "1.1.3";
        src = pkgs.fetchurl {
          url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-linux-${arch}.tar.gz";
          hash = {
            x64 = "sha256-XVOAsfkrGhmp33DTJykVDbClHIEPal0Nkq5UKQ7wXdI=";
            arm64 = "sha256-KtQpMxpxnS7tf0PHpR8s9zv2036CQtPd33gm+RkXYWs=";
          }.${arch};
        };
        sourceRoot = ".";
        nativeBuildInputs = with pkgs; [ patchelf makeWrapper ];
        dontPatchELF = true;
        dontStrip = true;
        installPhase = ''
          install -Dm755 Dsm.Provider.App $out/bin/.dsm-provider-unwrapped
          patchelf --set-interpreter "$(cat $NIX_CC/nix-support/dynamic-linker)" $out/bin/.dsm-provider-unwrapped
          makeWrapper $out/bin/.dsm-provider-unwrapped $out/bin/dsm-provider \
            --prefix LD_LIBRARY_PATH : "${pkgs.lib.makeLibraryPath runtimeLibs}"
        '';
      };
    });
  };
}
