# Non-flake entry point, required by nixpkgs' `update-source-version`.
#
# That tool evaluates `import ./.` from the repository root -- the path is hardcoded, with no
# flake support -- so nix/package.nix has to be reachable this way for the release updater in
# .github/workflows/update-flake.yml to work. It is not meant for general use; flake consumers
# should use `.#dsm-provider`.
#
# nixpkgs is resolved from flake.lock rather than from <nixpkgs>. Two reasons, the first found
# the hard way: cachix/install-nix-action does NOT set NIX_PATH, so `import <nixpkgs>` fails
# outright on a GitHub runner with "file 'nixpkgs' was not found in the Nix search path".
# Pointing NIX_PATH at a channel would have fixed that but would have introduced a second,
# unpinned nixpkgs alongside the flake's. This way flake.lock stays the only source of truth,
# and `nix build .#dsm-provider` and `nix-build -A dsm-provider` evaluate against the same
# nixpkgs.
#
# Both entry points call the same ./nix/package.nix, which is what keeps them in sync.
{
  system ? builtins.currentSystem,
  pkgs ?
    let
      lock = builtins.fromJSON (builtins.readFile ./flake.lock);
      node = lock.nodes.nixpkgs.locked;
    in
    import (builtins.fetchTree {
      type = "github";
      inherit (node) owner repo rev narHash;
    }) { inherit system; },
}:
{
  dsm-provider = pkgs.callPackage ./nix/package.nix { };
}
