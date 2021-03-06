class DashyItem

  AUTOGENERATED_TAG = "_autogenerated"

  attr_reader :title
  attr_reader :url
  attr_reader :icon
  attr_reader :tags

  def self.create_item_from_service(service)
    icon =
      if service.image_url.present?
        service.image_url 
      elsif service.icon.present?
        service.icon
      else
        "favicon-local"
      end

    return DashyItem.new(service.name, service.url, icon, [AUTOGENERATED_TAG, "host=#{service.hostname}"])
  end

  def initialize(title, url, icon, tags)
    @title = title
    @url = url
    @icon = icon
    @tags = tags
  end

  def ==(other)
    @title == other.title &&
    @url == other.url &&
    @icon == other.icon &&
    @tags == other.tags
  end

  def as_json(options={})
    {
      title: @title,
      url: @url,
      icon: @icon,
      tags: @tags
    }
  end

  def to_json(*options)
    as_json(*options).to_json(*options)
  end

  def autogenerated?
    @autogenerated ||= if @tags then @tags.include?(AUTOGENERATED_TAG) else false end
  end

  def hostname
    if @hostname
      return @hostname
    end

    @tags.each do |t|
      if /host=(.+)/.match(t)
        @hostname = Regexp.last_match.captures[0]
        break
      end
    end

    return @hostname
  end

  # def to_s
  #   "DashyItem:\n" +
  #   "  title: #{@title}\n" +
  #   "  url: #{@url}\n" +
  #   "  icon: #{@icon}\n" +
  #   "  tags: #{@tags}\n"
  # end

  # def encode_with(coder)
  #   coder["name"] = @name
  #   coder["url"] = @url
  #   coder["url"] = @url
  #   coder["url"] = @url
  #   coder["url"] = @url
  # end
end