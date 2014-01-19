Dir['solving_scripts/*.at'].each do |attempt_file|
  f = open(attempt_file).read
  puts attempt_file if f.ascii_only?
end
