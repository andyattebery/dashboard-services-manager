require 'active_support/core_ext/string/inflections'
require 'docker'
require 'uri'
require_relative '../models/service'

class DockerServiceProvider

  def initialize(config)
    @config = config
  end

  def get_services
    containers = Docker::Container.all(:all => true)
    services = containers.map{ |c| create_service_from_docker_api_container(c) }
    services
  end

  private

    def create_service(container_name, url, label_name, label_category, icon, image_path, opencontainers_image_title, hostname)
      default_service_config = @config.get_default_service_config(container_name.downcase)
      name_with_hostname_format_string = default_service_config["name_with_hostname_format_string"]
      default_category = default_service_config["category"]

      icon ||= default_service_config["icon"]
      image_path ||= default_service_config["image_path"]

      name =
        if label_name
          label_name
        elsif name_with_hostname_format_string
          name_with_hostname_format_string % hostname
        else
          container_name
        end

      category =
        if label_category
          label_category
        elsif default_category
          default_category
        else
          "uncategorized"
        end

      image_url =
        if image_path
          image_uri = URI.parse(image_path)
          if image_uri && image_uri.host
            image_uri.to_s
          else
            URI.join(url, image_uri.to_s).to_s
          end
        else
          nil
        end

      Service.new(
        name,
        url,
        category,
        icon,
        image_url,
        opencontainers_image_title,
        hostname
      )
    end

    def create_service_from_docker_api_container(container)
      container_name = container.info["Names"].first.delete_prefix("/").gsub("-", " ").titleize

      url = nil
      label_category = nil
      label_icon = nil
      label_image_path = nil
      label_name = nil
      label_traefik_router = nil
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
          next
        end
      end

      puts container_name
      puts "label_traefik_router: #{label_traefik_router}"
      puts "traefik_router_to_hosts"
      puts traefik_router_to_hosts

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

      create_service(
        container_name,
        url,
        label_name,
        label_category,
        label_icon,
        label_image_path,
        opencontainers_image_title,
        @config.hostname
      )
    end
end