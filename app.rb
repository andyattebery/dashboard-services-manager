require 'docker'
require 'http'
require 'json'
require 'yaml'
require 'sinatra/base'
# require 'sinatra/config_file'

require_relative 'config'
require_relative 'dashboard_managers/dashy_dashboard_manager'
require_relative 'service_providers/docker_service_provider'

class DockerServicesApi < Sinatra::Base

  @config = Config.new
  @service_provider = DockerServiceProvider.new
  @config_manager = DashyDashboardManager.new(@config)

  get "/containers" do
    get_services
  end

  get "/config" do
    get_config
  end

  get "/update-config" do
    config = Config.new
    service_provider = DockerServiceProvider.new
    config_manager = DashyDashboardManager.new(config)

    services = service_provider.get_services
    config_manager.save_to_config_file(services)

    "Updated"
  end

  def get_services
    config = Config.new
    service_provider = DockerServiceProvider.new
    config_manager = DashyDashboardManager.new(config)

    services = service_provider.get_services
    section_items_map = config_manager.create_section_items_map(services)
    section_items_map.to_yaml
  end

  def get_config
    config = Config.new
    config_manager = DashyDashboardManager.new(config)
    section_items_map = config_manager.get_section_items_map
    section_items_map.to_json
  end

end