class Config

  attr_reader :dashy_config_file_path
  attr_reader :docker_label_prefix
  attr_reader :hostname

  def initialize
    @dashy_config_file_path = "/app/dashy_config.yaml"
    @docker_label_prefix = "dcm"
    @hostname = "chi"
  end

end