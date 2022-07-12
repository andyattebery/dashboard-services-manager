class Service

  attr_reader :name
  attr_reader :url
  attr_reader :category
  attr_reader :icon
  attr_reader :image_url
  attr_reader :opencontainers_image_title
  attr_reader :hostname

  def initialize(name, url, category, icon, image_url, opencontainers_image_title, hostname)
    @name = name
    @url = url
    @category = category
    @icon = icon
    @image_url = image_url
    @opencontainers_image_title = opencontainers_image_title
    @hostname = hostname
  end

  def as_json(options={})
    {
      name: @name,
      url: @url,
      category: @category,
      icon: @icon,
      image_url: @image_url,
      opencontainers_image_title: @opencontainers_image_title
      hostname: @hostname
    }
  end

  def to_json(*options)
    as_json(*options).to_json(*options)
  end

end