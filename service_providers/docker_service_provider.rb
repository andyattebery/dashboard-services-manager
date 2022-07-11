require 'uri'
require 'active_support/core_ext/string/inflections'

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

    def create_service_from_docker_api_container(container)
      url = ""
      category = "Uncategorized"
      icon = ""
      image_uri = nil

      container_name = container.info["Names"].first.delete_prefix("/").gsub("-", " ").titleize
      dcm_name = ""
      opencontainers_image_title = ""

      container.info["Labels"].each do |k,v|
        if /^traefik\.http\.routers.*\.rule/.match?(k) && /^Host\((.+)\)/.match(v)
          urls = Regexp.last_match.captures[0].split(",").map { |h| h.gsub("`", "")}
          url = "https://#{urls.first}"
        elsif k == "org.opencontainers.image.title"
          opencontainers_image_title = v
        elsif k == "#{@config.docker_label_prefix}.name"
          dcm_name = v
        elsif k == "#{@config.docker_label_prefix}.category"
          category = v
        elsif k == "#{@config.docker_label_prefix}.icon"
          icon = v
        elsif k == "#{@config.docker_label_prefix}.image_path"
          image_uri = URI.parse(v)
        end
      end

      name =
        if !dcm_name.empty?
          dcm_name
        else
          container_name
        end

      image_url = nil

      if image_uri != nil
        if (image_uri.host == nil)
          image_url = URI.join(url, image_uri.to_s).to_s
        else
          image_url = image_uri.to_s
        end
      end

      service = Service.new(
        name,
        url,
        category,
        icon,
        image_url,
        opencontainers_image_title,
        container.info["Names"],
        container.info["Labels"])
    end
end