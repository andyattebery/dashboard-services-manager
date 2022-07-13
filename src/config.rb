class Config

  attr_reader :api_url
  attr_reader :dashy_config_file_path
  attr_reader :docker_label_prefix
  attr_reader :hostname
  attr_reader :ignored_service_names
  attr_reader :section_icons

  def initialize
    config_hash = YAML.load_file("#{ENV['APP_CONFIG_DIR']}/config.yaml")

    @hostname = ENV["HOSTNAME"]

    @api_url = config_hash["api_url"]
    @dashy_config_file_path = config_hash["dashy_config_file_path"]
    @docker_label_prefix = config_hash["docker_label_prefix"] || "dcm"
    @ignored_service_names = config_hash["ignored_service_names"] || []
    @default_service_configs = config_hash["default_service_configs"] || {}
    @section_icons = config_hash["section_icons"] || {}
  end

  def get_default_service_config(name)
    default_service_config_hash = @default_service_configs[name] || {}
    return {
      "name_with_hostname_format_string" => default_service_config_hash["name_with_hostname_format_string"],
      "category" => default_service_config_hash["category"],
      "icon" => default_service_config_hash["icon"],
      "image_path" => default_service_config_hash["image_path"]
    }
  end

end