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
