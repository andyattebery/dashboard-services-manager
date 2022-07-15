require 'active_support/core_ext/string/inflections'
require 'docker'
require 'uri'
require_relative '../service/service'
require_relative '../service/service_factory'

class DockerServiceProvider

  def initialize(config, service_factory)
    @config = config
    @service_factory = service_factory
  end

  def get_services(include_ignored=false)
    containers = Docker::Container.all()
    services = containers.map{ |c| create_service_from_docker_api_container(c) }
    if !include_ignored
      services = services.select{ |s| !s.ignore? }
    end
    services
  end

  private

    def create_service_from_docker_api_container(container)

      container_name = container.info["Names"].first.delete_prefix("/").gsub("-", " ").titleize

      url = nil
      label_category = nil
      label_icon = nil
      label_image_path = nil
      label_name = nil
      label_traefik_router = nil
      label_ignore = false
      opencontainers_image_title = nil
      traefik_router_to_hosts = {}

      container.info["Labels"].each do |k,v|
        if (router_match = /^traefik\.http\.routers\.(.*)\.rule/.match(k)) &&
           (url_match = /^Host\((.+)\)/.match(v))
          traefik_router_to_hosts[router_match.captures[0]] = url_match.captures[0]
        elsif k == "org.opencontainers.image.title"
          opencontainers_image_title = v
        elsif k == "#{@config.docker_label_prefix}.name"
          label_name = v
        elsif k == "#{@config.docker_label_prefix}.category"
          label_category = v
        elsif k == "#{@config.docker_label_prefix}.icon"
          label_icon = v
        elsif k == "#{@config.docker_label_prefix}.image_path"
          label_image_path = v
        elsif k == "#{@config.docker_label_prefix}.traefik_router"
          label_traefik_router = v
        elsif k == "#{@config.docker_label_prefix}.ignore" &&
          v == "true"
          label_ignore = true
        end
      end

      name = label_name ? label_name : container_name
      url = get_url(label_traefik_router, traefik_router_to_hosts)

      return Service.new(
        name,
        url,
        label_category,
        label_icon,
        label_image_path,
        opencontainers_image_title,
        @config.hostname,
        label_ignore
      )
    end

    def get_url(label_traefik_router, traefik_router_to_hosts)
      traefik_router_host =
        if traefik_router_to_hosts.any?
          traefik_router_hosts = label_traefik_router ?
            traefik_router_to_hosts[label_traefik_router].split(",")  :
            traefik_router_to_hosts.values.map { |rhs| rhs.split(",") }.flatten
          traefik_router_hosts.first
        else
          nil
        end

      url =
        if traefik_router_host
          clean_router_host = traefik_router_host.gsub("`", "")
          @config.are_service_hosts_https ?
            "https://#{clean_router_host}" :
            "https://#{clean_router_host}"
        else
          nil
        end

      return url
    end

end