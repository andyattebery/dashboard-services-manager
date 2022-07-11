class DashyItem
  attr_reader :title
  attr_reader :url
  attr_reader :icon
  attr_reader :tags

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