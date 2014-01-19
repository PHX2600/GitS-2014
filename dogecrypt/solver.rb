require 'open3'

entries = File.readlines "american-english-small <-Password is in here somewhere"

dogecrypt_file = 'dogecrypt-b36f587051faafc444417eb10dd47b0f30a52a0b'
full_path = File.join __dir__, dogecrypt_file

entries.each do |word|
    word.strip!
    open("solving_scripts/#{word}.vim", 'w').write("#{word}:set key=:saveas #{word}.at:q\n")
end
