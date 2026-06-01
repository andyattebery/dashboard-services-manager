{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.11";

  outputs = { self, nixpkgs }:
  let
    forAllSystems = nixpkgs.lib.genAttrs [ "x86_64-linux" "aarch64-linux" ];
  in {
    nixosModules.dsm-provider = { pkgs, lib, ... }: {
      imports = [ ./nix/module.nix ];
      services.dsm-provider.package = lib.mkDefault self.packages.${pkgs.system}.dsm-provider;
    };

    packages = forAllSystems (system:
    let
      pkgs = nixpkgs.legacyPackages.${system};
      arch = if system == "aarch64-linux" then "arm64" else "x64";
      runtimeLibs = with pkgs; [ icu openssl stdenv.cc.cc.lib ];
    in {
      dsm-provider = pkgs.stdenv.mkDerivation rec {
        pname = "dsm-provider";
        version = "1.2.0";
        src = pkgs.fetchurl {
          url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-linux-${arch}.tar.gz";
          hash = {
            x64 = "sha256-5iHrrX3PlsH96F5osMraCF/r5BmiPK6GwXI+b/38T4A=";
            arm64 = "sha256-evQRRzH8jByN3omtHp5ioIq/CBX761b2t4SnCEoDOCk=";
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
