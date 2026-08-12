{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.11";

  outputs = { self, nixpkgs }:
  let
    forAllSystems = nixpkgs.lib.genAttrs [ "x86_64-linux" "aarch64-linux" ];
  in {
    # `key` is what makes this module safe to import from more than one place. NixOS dedups
    # imports by key, and a path's key is its path -- but this is a lambda (it needs `pkgs`
    # to pick the package), and a lambda gets a fresh key per import site. Without the key,
    # a config importing it twice -- say a host module and a role module that both want it --
    # defines services.dsm-provider.package twice, and that option has no default and a
    # unique merge, so evaluation fails with "defined multiple times".
    # lib.setDefaultModuleLocation does NOT fix this: it sets _file, which only affects
    # error messages, not the dedup key.
    nixosModules.dsm-provider = {
      key = "dsm-provider";
      imports = [
        ./nix/module.nix
        ({ pkgs, lib, ... }: {
          services.dsm-provider.package =
            lib.mkDefault self.packages.${pkgs.stdenv.hostPlatform.system}.dsm-provider;
        })
      ];
    };

    # Regression test for the `key` above: this module must survive being imported twice.
    #
    # It forces services.dsm-provider.package specifically. That is the option that actually
    # conflicts, and forcing something cheaper -- `.enable`, say -- succeeds even when the
    # module IS broken, so it would be a check that can never fail. Measured, not assumed.
    #
    # It also has to live in `checks` to be worth anything: `nix flake check` does not
    # evaluate nixosModules at all. nix's checkModule only calls forceValue on the attribute,
    # which for a lambda yields the lambda unapplied, so a broken module passes it. Only
    # `checks.<system>.<name>` is really built.
    checks = forAllSystems (system: {
      module-imports-twice =
        let
          sys = nixpkgs.lib.nixosSystem {
            modules = [
              self.nixosModules.dsm-provider
              self.nixosModules.dsm-provider
              {
                nixpkgs.hostPlatform = system;
                system.stateVersion = nixpkgs.lib.trivial.release;
                services.dsm-provider = {
                  enable = true;
                  apiUrl = "http://example.invalid";
                };
              }
            ];
          };
        in
        nixpkgs.legacyPackages.${system}.writeText "dsm-provider-imports-twice-ok"
          sys.config.services.dsm-provider.package.pname;
    });

    packages = forAllSystems (system:
    let
      pkgs = nixpkgs.legacyPackages.${system};
      arch = if system == "aarch64-linux" then "arm64" else "x64";
      runtimeLibs = with pkgs; [ icu openssl stdenv.cc.cc.lib ];
    in {
      dsm-provider = pkgs.stdenv.mkDerivation rec {
        pname = "dsm-provider";
        version = "1.2.6";
        src = pkgs.fetchurl {
          url = "https://github.com/andyattebery/dashboard-services-manager/releases/download/${version}/dsm-provider-${version}-linux-${arch}.tar.gz";
          hash = {
            x64 = "sha256-S/KFdB34IulPx0s9zJ8Bju2NOun3nvk8HTVXwuUeZKk=";
            arm64 = "sha256-bgkh4PTbZf5rKQ8/3330Gs6HWQNdC8Tb7lsZgqtfT5o=";
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
