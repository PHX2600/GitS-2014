require 'json'
require 'open-uri'
require 'pp'

our_points = 0
our_rank = 0

scores = JSON.parse(open('https://2014.ghostintheshellcode.com/ajax/getScores?limit=100').read)['scores']
scores.each_with_index do |team, index|
    if team['name'] == 'Pi Backwards'
        our_points = team['points']
        our_rank = index+1
    end
end

puts "We're rank #{our_rank} with #{our_points} points! KEEP GOING!"
