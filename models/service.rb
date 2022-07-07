class Service
  attr_reader :name
  attr_reader :hostname
  attr_reader :category
  attr_reader :icon
  attr_reader :names
  attr_reader :labels

  def initialize(name, hostname, category, icon, names, labels)
    @name = name
    @category = category
    @icon = icon
    @names = names
    @labels = labels
    @hostname = hostname
  end

  def hostname?
    !@hostname.nil? && !@hostname.empty?
  end

  def uncategorized?
    !@category.nil? && !@category.empty?
  end

  def icon?
    !@icon.nil? && !@icon.empty?
  end

  def to_s
    "name: #{@name} | hostname: #{@hostname} | category: #{@category}"
  end

  def to_s_debug
    "#{to_s}|\nnames: #{@names.join(",")}|\nlabels: #{@labels.to_json}"
  end
end