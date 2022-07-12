require 'json'
require 'yaml'
require 'sinatra/base'
require 'sinatra/json'
# require 'sinatra/config_file'

require_relative 'config'
require_relative 'dashboard_managers/dashy_dashboard_manager'
require_relative 'models/service'
require_relative 'service_providers/docker_service_provider'

class Api < Sinatra::Base

  def initialize(app = nil, **kwargs)
    super(app, **kwargs)

    @config = Config.new
    @service_provider = DockerServiceProvider.new(@config)
    @dashboard_manager = DashyDashboardManager.new(@config)

    yield self if block_given?
  end

  get "/dashboard-config" do
    headers "Content-Type" => "text/x.yaml"
    @dashboard_manager.get_dashboard_config.to_yaml
  end

  get "/dashboard-config/sections" do
    sections = @dashboard_manager.get_sections_from_dashboard_config
    json sections
  end

  get "/dashboard-config/updated-sections" do
    services = @service_provider.get_services
    sections = @dashboard_manager.get_updated_sections(services)
    json sections
  end

  get "/services" do
    services = @service_provider.get_services
    json services
  end

  get "/dashboard-config/update-from-local-services" do
    headers "Content-Type" => "text/x.yaml"
    services = @service_provider.get_services
    updated_sections = @dashboard_manager.update_dashboard_config_file(services)
    updated_sections.to_yaml
  end

  post "/dashboard-config/update-from-services" do
    service_hashes = JSON.parse(request.body.read)
    services = service_hashes.map do |sh|
      Service.new(
        sh["name"],
        sh["url"], 
        sh["category"], 
        sh["icon"], 
        sh["image_url"], 
        sh["opencontainers_image_title"], 
        sh["hostname"])
    end

    updated_sections = @dashboard_manager.update_dashboard_config_file(services)
    json updated_sections
  end

end