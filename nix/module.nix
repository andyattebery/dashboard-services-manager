{ config, lib, pkgs, ... }:

let
  cfg = config.services.dsm-provider;
  yamlFormat = pkgs.formats.yaml {};

  hasServices = cfg.services != [];

  autoYamlProvider = {
    type = "YamlFile";
    hostname = null;
    areServiceHostsHttps = true;
    dockerLabelPrefix = null;
    traefikApiUrl = null;
    servicesYamlFilePath = "./provider-services.yaml";
  };

  allProviders = cfg.providers ++ lib.optional hasServices autoYamlProvider;

  needsDocker = lib.any (p: p.type == "Docker" || p.type == "Swarm") allProviders;

  providerToAttrs = p: lib.filterAttrs (_: v: v != null) {
    ServicesProviderType = p.type;
    Hostname = p.hostname;
    AreServiceHostsHttps = p.areServiceHostsHttps;
    DockerLabelPrefix = p.dockerLabelPrefix;
    TraefikApiUrl = p.traefikApiUrl;
    ServicesYamlFilePath = p.servicesYamlFilePath;
  };

  serviceToAttrs = s: lib.filterAttrs (_: v: v != null) {
    inherit (s) name url hostname category icon service_defaults_name;
  };

  configDir = pkgs.linkFarm "dsm-provider-config" ([
    {
      name = "provider-config.yaml";
      path = yamlFormat.generate "provider-config.yaml" {
        ProviderOptions = lib.filterAttrs (_: v: v != null) {
          ApiUrl = cfg.apiUrl;
          ApiKey = cfg.apiKey;
          RefreshInterval = cfg.refreshInterval;
          ServicesProviders = map providerToAttrs allProviders;
        };
      };
    }
  ] ++ lib.optional hasServices {
    name = "provider-services.yaml";
    path = yamlFormat.generate "provider-services.yaml" (map serviceToAttrs cfg.services);
  });

  providerSubmodule = lib.types.submodule {
    options = {
      type = lib.mkOption {
        type = lib.types.enum [ "Docker" "Swarm" "Traefik" "YamlFile" ];
      };
      hostname = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
      areServiceHostsHttps = lib.mkOption {
        type = lib.types.bool;
        default = true;
      };
      dockerLabelPrefix = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
      traefikApiUrl = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
      servicesYamlFilePath = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
    };
  };

  serviceSubmodule = lib.types.submodule {
    options = {
      name = lib.mkOption { type = lib.types.str; };
      url = lib.mkOption { type = lib.types.str; };
      hostname = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
      category = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
      icon = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
      service_defaults_name = lib.mkOption {
        type = lib.types.nullOr lib.types.str;
        default = null;
      };
    };
  };

in {
  options.services.dsm-provider = {
    enable = lib.mkEnableOption "Dashboard Services Manager Provider";

    package = lib.mkOption {
      type = lib.types.package;
    };

    apiUrl = lib.mkOption {
      type = lib.types.str;
    };

    apiKey = lib.mkOption {
      type = lib.types.nullOr lib.types.str;
      default = null;
    };

    refreshInterval = lib.mkOption {
      type = lib.types.nullOr lib.types.str;
      default = null;
    };

    providers = lib.mkOption {
      type = lib.types.listOf providerSubmodule;
      default = [];
    };

    services = lib.mkOption {
      type = lib.types.listOf serviceSubmodule;
      default = [];
    };
  };

  config = lib.mkIf cfg.enable {
    users.users.dsm-provider = {
      isSystemUser = true;
      group = "dsm-provider";
      extraGroups = lib.optional needsDocker "docker";
    };
    users.groups.dsm-provider = {};

    systemd.services.dsm-provider = {
      description = "Dashboard Services Manager Provider";
      after = [ "network-online.target" ] ++ lib.optional needsDocker "docker.service";
      wants = [ "network-online.target" ];
      requires = lib.optional needsDocker "docker.service";
      wantedBy = [ "multi-user.target" ];
      serviceConfig = {
        ExecStart = "${cfg.package}/bin/dsm-provider";
        WorkingDirectory = configDir;
        Restart = "always";
        User = "dsm-provider";
        Group = "dsm-provider";
        SupplementaryGroups = lib.optional needsDocker "docker";
      };
      restartTriggers = [ configDir ];
    };
  };
}
