require 'uri'
require 'active_support/core_ext/string/inflections'

require_relative '../models/service'

class DockerServiceProvider
  def get_services
    containers = Docker::Container.all(:all => true)

    response = ""

    container_infos = []

    containers.each do |c|

      container_info = create_service_from_docker_api_container(c)

      unless container_info.hostname?
        next
      end

      container_infos << container_info

      puts container_info.to_s
    end

    container_infos
  end

  def create_service_from_docker_api_container(container)
    hostname = ""
    category = "Uncategorized"
    icon = ""
    icon_uri = nil

    container_name = container.info["Names"].first.delete_prefix("/").gsub("-", " ").titleize
    dcm_name = ""
    opencontainers_image_title = ""

    container.info["Labels"].each do |k,v|
      if /^traefik\.http\.routers.*\.rule/.match?(k) && /^Host\((.+)\)/.match(v)
        hostnames = Regexp.last_match.captures[0].split(",").map { |h| h.gsub("`", "")}
        hostname = "https://#{hostnames.first}"
      elsif /org\.opencontainers\.image\.title/.match?(k)
        opencontainers_image_title = v
      elsif /dcm\.name/.match?(k)
        dcm_name = v
      elsif /dcm\.category/.match?(k)
        category = v
      elsif /dcm\.icon/.match?(k)
        if v.include?("/")
          begin
            icon_uri = URI.parse(v)
          rescue URI::InvalidURIError
            icon = v
          end
        else
          icon = v
        end
      end
    end

    name =
      if !dcm_name.empty?
        dcm_name
      elsif !opencontainers_image_title.empty?
        opencontainers_image_title
      else
        container_name
      end

    if icon_uri != nil
      if (icon_uri.host == nil)
        icon = URI.join(hostname, icon_uri.to_s).to_s
      else
        icon = icon_uri.to_s
      end
    end

    service = Service.new(name, hostname, category, icon, container.info["Names"], container.info["Labels"])
  end
end