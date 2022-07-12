require 'net/http'
require 'uri'
require_relative 'config'
require_relative 'service_providers/docker_service_provider'

class Provider

  def initialize
    @config = Config.new
    @service_provider = DockerServiceProvider.new(@config)
  end

  def update_api_with_services
    services = @service_provider.get_services

    puts services.to_json

    update_services_uri = URI.join(@config.api_url, "/dashboard-config/update-from-services")
    response = Net::HTTP.post(update_services_uri,
                              services.to_json,
                              "Content-Type" => "application/json")

    puts response.message
    puts response.body

    if response.is_a?(Net::HTTPSuccess)
      puts response.body
    end
  end

end

Provider.new.update_api_with_services
