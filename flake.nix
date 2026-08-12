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

    # The derivation lives in nix/package.nix, not here: it is the file update-flake.yml
    # rewrites on each release, and keeping it separate means those seds have a small,
    # stable target instead of the whole flake.
    packages = forAllSystems (system:
    let
      pkgs = nixpkgs.legacyPackages.${system};
    in {
      dsm-provider = pkgs.callPackage ./nix/package.nix { };
    });
  };
}
