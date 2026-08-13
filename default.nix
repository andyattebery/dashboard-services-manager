# Non-flake entry point, required by nixpkgs' `update-source-version`.
#
# That tool evaluates `import ./.` from the repository root -- the path is hardcoded, with no
# flake support -- so `nix/package.nix` has to be reachable this way for the release updater
# in .github/workflows/update-flake.yml to work. It is not intended for general use; flake
# consumers should use `.#dsm-provider`.
#
# Both this and flake.nix call the same ./nix/package.nix, which is what keeps them in sync.
{
  system ? builtins.currentSystem,
  pkgs ? import <nixpkgs> { inherit system; },
}:
{
  dsm-provider = pkgs.callPackage ./nix/package.nix { };
}
