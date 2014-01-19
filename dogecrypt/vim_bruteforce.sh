#!/bin/sh

mkdir solving_scripts
ruby ./solver.rb

cd solving_scripts
for i in *.vim; do vim -s $i ../dogecrypt-b36f587051faafc444417eb10dd47b0f30a52a0b; done

cd ../
ruby ascii_chars.rb
