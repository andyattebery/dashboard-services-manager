class Config

  attr_reader :api_url
  attr_reader :dashy_config_file_path
  attr_reader :docker_label_prefix
  attr_reader :hostname

  def initialize
    @api_url = "https://dashboard-services-manager.omegaho.me"
    @dashy_config_file_path = "#{ENV['APP_CONFIG_DIR']}/dashy_config.yaml"
    @docker_label_prefix = "dcm"
    @hostname = ENV["HOSTNAME"]
    @ignored_service_names = 
      [
        "Dashy",
        "dashboard-services-manager"
      ]
    
  end

end