require_relative 'service'

class ServiceFactory

  def initialize(config)
    @config = config
  end

  def create_from_json_hash(json_hash)
    return Service.new(
      json_hash["name"],
      json_hash["url"],
      json_hash["category"],
      json_hash["icon"],
      json_hash["image_url"],
      json_hash["opencontainers_image_title"],
      json_hash["hostname"],
      json_hash["ignored"]
    )
  end

  def create_with_default_service_config(service)
    default_service_config = @config.get_default_service_config(container_name.downcase)

    name_with_hostname_format_string = default_service_config["name_with_hostname_format_string"]
    default_category = default_service_config["category"]
    default_icon = default_service_config["icon"]
    default_image_path = default_service_config["image_path"]

    name = name_with_hostname_format_string ?
      name_with_hostname_format_string % hostname :
      service.name

    category =
      if service.category
        service.category
      elsif default_category
        default_category
      else
        "uncategorized"
      end

    icon = service.icon ? service.icon : default_icon

    image_path = service.image_path ? service.image_path : default_image_path

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

    return Service.new(
      name,
      url,
      category,
      icon,
      image_url,
      service.opencontainers_image_title,
      service.hostname
    )
  end

  def create_with_default_service_config_from_json_hash(json_hash)
    service = create_from_json_hash(json_hash)
    return create_with_default_service_config(service)
  end

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

    return Service.new(
      name,
      url,
      category,
      icon,
      image_url,
      opencontainers_image_title,
      hostname
    )
  end

end