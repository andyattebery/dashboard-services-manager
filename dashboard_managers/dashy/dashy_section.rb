class DashySection

  attr_reader :name
  attr_reader :icon
  attr_reader :items

  def initialize(name, icon, items)
    @name = name
    @icon = icon
    @items = items
  end

  def as_json(options={})
    {
      name: @name,
      icon: @icon,
      items: @items
    }
  end

  def to_json(*options)
    as_json(*options).to_json(*options)
  end

  # def to_s
  #   "DashySection:\n" +
  #   "  name: #{@name}\n" +
  #   "  icon: #{@icon}\n" +
  #   "  items: #{@items}\n"
  # end

end