require 'net/http'

require_relative 'service_providers/docker_service_provider'

class Provider

  def initialize
    @config = Config.new
    @service_provider = DockerServiceProvider.new(@config)
  end

  def update_api_with_services
    services = @service_provider.get_services
    update_services_uri = URI.new(@config.api_url, "/dashboard-config/update-from-services")
    
  end

end
