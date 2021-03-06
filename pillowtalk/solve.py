#!/usr/bin/python

import array
import string

#change as key characters are discovered
key = [
0xdb, 0x57, 0x07, 0xe4, 0xa8, 0x11, 0x6c, 0x8c, 
0xA0, 0x1e, 0x04, 0x7e, 0xcc, 0xbe, 0x1e, 0xde,
0xc8, 0xee, 0x28, 0xe0, 0x90, 0x8a, 0x05, 0xec, 
0xa4, 0xbb, 0xf0, 0x91, 0x3a, 0xee, 0x4d, 0xf7, 
0xb3, 0xcd, 0xa0, 0x89, 0x59, 0x2e, 0x96, 0x76, 
0x77, 0x70, 0xfb, 0x0a, 0x77, 0x23, 0x22, 0x13, 
0xc4, 0x99, 0x4a, 0xcc, 0xd9, 0xf7, 0x75, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]

ciphertexts = [

#1
[0xb3, 0x3e, 0x0d ],

#2
[0xbb, 0x3e, 0x63, 0x84, 0xa2 ],

#3
[
0xa2, 0x32, 0x66, 0x8c, 0x84, 0x31, 0x00, 0xe5, 
0xcb, 0x7b, 0x24, 0x17, 0xeb, 0xda, 0x3e, 0xad, 
0xb1, 0x9d, 0x5c, 0x85, 0xfd, 0xaa, 0x60, 0x9a, 
0xc1, 0xc9, 0x89, 0xe5, 0x52, 0x87, 0x23, 0x90, 
0x93, 0xa2, 0xd5, 0xfd, 0x53 ],

#4
[
0xfe, 0x2f, 0x22, 0x9c, 0x8d, 0x69, 0x49, 0xff, 
0x85, 0x6d, 0x21, 0x0d, 0xe9, 0xcd, 0x3b, 0xad, 
0xed, 0x80, 0x0d, 0x8e, 0xb5, 0xe4, 0x20, 0x82, 
0x81, 0xd5, 0xd5, 0xff, 0x1f, 0x80, 0x68, 0x99, 
0x96, 0xa3, 0xaa ],

#5
[
0xeb, 0x2f, 0x33, 0xd5, 0x9c, 0x20, 0x58, 0xbd, 
0x94, 0x2f, 0x30, 0x4f, 0xf8, 0x8f, 0x2a, 0xef, 
0xfc, 0xdf, 0x22 ],

#6
[
0xac, 0x38, 0x68, 0x90, 0x84, 0x31, 0x00, 0xe3, 
0xcf, 0x75, 0x77, 0x5e, 0xa0, 0xd7, 0x75, 0xbb, 
0xe8, 0x87, 0x45, 0xc0, 0xf1, 0xe6, 0x69, 0xcc, 
0xc2, 0xd2, 0x9e, 0xf8, 0x49, 0x86, 0x28, 0x93, 
0x93, 0xa5, 0xc5, 0xfb, 0x3c, 0x0e, 0xe2, 0x1e, 
0x12, 0x1e, 0xf1 ],

#7
[
0xa8, 0x38, 0x2b, 0xc4, 0xdc, 0x79, 0x09, 0xac, 
0xd0, 0x71, 0x6d, 0x10, 0xb8, 0x9e, 0x71, 0xb8, 
0xe8, 0x9a, 0x40, 0x89, 0xe3, 0xaa, 0x66, 0x84, 
0xc5, 0xd7, 0x9c, 0xf4, 0x54, 0x89, 0x28, 0xd7, 
0xda, 0xbe, 0x80, 0xfd, 0x36, 0x0e, 0xfb, 0x17, 
0x1c, 0x15, 0xdb, 0x79, 0x18, 0x4e, 0x47, 0x7c, 
0xaa, 0xfc, 0x6a, 0xbf, 0xb0, 0x83, 0x55, 0xb3, 
0xca, 0xc1, 0xad, 0x19, 0xd0, 0xbc, 0xe8, 0xaa, 
0xd2, 0x75, 0x61, 0xa8, 0x02, 0x6c, 0x5b, 0xb2, 
0x2e, 0xeb, 0xbf, 0xe5, 0x7d, 0x78, 0x34, 0xcc, 
0x7c, 0xa8, 0x9a, 0xd1, 0xd7, 0xa6, 0x30, 0x79, 
0xa3, 0xde, 0x05, 0x16, 0x8b, 0x4b, 0x5a, 0x17, 
0xc1, 0xf7, 0x39, 0x74 ],

#8
[
0xab, 0x36, 0x63, 0xc4, 0xdf, 0x79, 0x0d, 0xf8, 
0xd3, 0x21, 0x0e ],

#9
[
0xbf, 0x38, 0x27, 0x9d, 0xc7, 0x64, 0x4c, 0xf9, 
0xce, 0x7a, 0x61, 0x0c, 0xbf, 0xca, 0x7f, 0xb0, 
0xac, 0xd1, 0x22 ],

#10
[
0xb8, 0x38, 0x6a, 0x94, 0xdd, 0x65, 0x09, 0xfe, 
0xd3, 0x21, 0x24, 0x07, 0xa9, 0xcd, 0x3e, 0xad, 
0xa7, 0x83, 0x4d, 0x94, 0xf9, 0xe7, 0x60, 0x9f, 
0x8a, 0x9b, 0x93, 0xe3, 0x43, 0x9e, 0x39, 0x98, 
0x8c, 0xed, 0xce, 0xe6, 0x29, 0x4b, 0x9c ],

#11
[
0xa8, 0x38, 0x2b, 0xc4, 0xdc, 0x79, 0x09, 0xac, 
0xd0, 0x6c, 0x6b, 0x1c, 0xa0, 0xdb, 0x73, 0xfe, 
0xbf, 0x87, 0x5c, 0x88, 0xb0, 0xfe, 0x6d, 0x85, 
0xd7, 0x9b, 0x83, 0xf4, 0x48, 0x98, 0x24, 0x94, 
0xd6, 0xed, 0xc9, 0xfa, 0x79, 0x47, 0xe2, 0x56, 
0x05, 0x15, 0x8e, 0x79, 0x12, 0x50, 0x02, 0x67, 
0xac, 0xfc, 0x6a, 0xbf, 0xb8, 0x9a, 0x10, 0xe7, 
0xda, 0xcb, 0xad, 0x5c, 0x9b, 0xb8, 0xff, 0xe2, 
0xd2, 0x70, 0x6a, 0xaf, 0x4d, 0x71, 0x51, 0xe7, 
0x6f, 0xe4, 0xb2, 0xab, 0x29, 0x64, 0x30, 0x87, 
0x6d, 0xff, 0x81, 0x8b, 0xcc, 0xef, 0x3e, 0x75, 
0xf3, 0xc6, 0x01, 0x00, 0x8b, 0x4a, 0x4c, 0x0e, 
0xd2, 0xe1, 0x6a, 0x1f, 0x46, 0xd8, 0x7a, 0x9f, 
0xf2, 0xa1, 0x95, 0x4e, 0x5e, 0x9c, 0x21, 0x1e, 
0x00, 0xa1, 0x1f, 0x77, 0x9b, 0xe2, 0x01, 0x49, 
0x16, 0x16, 0x88, 0x5c, 0x24, 0x84, 0x74, 0xb7, 
0xbb, 0x78, 0x5e, 0x66, 0x79, 0xdc, 0xb9, 0x3f, 
0x41, 0xa5, 0x36, 0x20, 0x7b, 0x64, 0x1d, 0x58, 
0x56, 0x5b, 0xee, 0x85, 0xa0, 0xd8, 0x4f, 0x62, 
0x27, 0xd1, 0xad, 0xa1, 0x29, 0xdd, 0x89 ],

#12
[
0xb6, 0x36, 0x7e, 0x86, 0xcd, 0x31, 0x05, 0xac, 
0xce, 0x7b, 0x61, 0x1a, 0xec, 0xca, 0x71, 0xfe, 
0xba, 0x8b, 0x05, 0x92, 0xf5, 0xa7, 0x72, 0x8d, 
0xd0, 0xd8, 0x98, 0xb1, 0x52, 0x9a, 0x39, 0x87, #32
0x89, 0xe2, 0x8f, 0xfe, 0x2e, 0x59, 0xb8, 0x0f, #40
0x18, 0x05, 0x8f, 0x7f, 0x15, 0x46, 0x0c, 0x70, #48
0xab, 0xf4, 0x65, 0xbb, 0xb8, 0x83, 0x16, 0xaf, 
0x9d, 0xd2, 0xe2, 0x25, 0xb5, 0x9f, 0xe0, 0xaf, 
0x9f, 0x67, 0x29, 0x94, 0x09, 0x67, 0x34 ],

#13
[
0xb6, 0x36, 0x7e, 0x86, 0xcd, 0x1b ],

#14
[
0xb4, 0x25, 0x27, 0x89, 0xc9, 0x68, 0x0e, 0xe9, 
0x80, 0x77, 0x24, 0x09, 0xa5, 0xd2, 0x72, 0xfe, 
0xa2, 0x9b, 0x5b, 0x94, 0xb0, 0xfd, 0x64, 0x98, 
0xc7, 0xd3, 0xd0, 0xe5, 0x52, 0x87, 0x3e, 0xd7, 
0xdc, 0xa3, 0xc5, 0xa9, 0x30, 0x40, 0xb6, 0x1a, 
0x18, 0x1f, 0x8b, 0x2a, 0x11, 0x4c, 0x50, 0x33, #48
0xac, 0xf6, 0x3f, 0xbe, 0xaa, 0xd7, 0x06, 0xae, 
0xcc, 0xc7, 0xba, 0x5c, 0x99, 0xa9, 0xa6, 0xa7, 
0x81, 0x31, 0x73, 0xaa, 0x14, 0x28, 0x5c, 0xf7, 
0x3b, 0xf3, 0xb6, 0xb7, 0x29, 0x78, 0x25, 0x98, 
0x78, 0xe5, 0xda, 0xd3, 0xd4, 0xb8, 0x2a, 0x32, 
0xfa, 0xc1, 0x11, 0x06, 0xde, 0x5c, 0x4c, 0x58, 
0xc5, 0xfd, 0x27, 0x51, 0x5f, 0xdd, 0x2e, 0x84, 
0xf5, 0xec, 0xc3, 0x07, 0x7e, 0x88, 0x79, 0x57, 
0x59, 0xf8, 0x1b, 0x58, 0xa2, 0xc0, 0x17, 0x31 ],

[
0xbf, 0x38, 0x69, 0xc3, 0xdc, 0x31, 0x08, 0xe5, 
0xd3, 0x6a, 0x76, 0x1f, 0xaf, 0xca, 0x3e, 0xb3, 
0xad, 0xc2, 0x08, 0xa9, 0xb7, 0xe7, 0x25, 0x98, 
0xd6, 0xc2, 0x99, 0xff, 0x5d, 0xce, 0x39, 0x98, 
0x93, 0xa0, 0xc1, 0xe2, 0x3c, 0x0e, 0xf7, 0x56, 
0x14, 0x18, 0x9a, 0x66, 0x1b, 0x46, 0x4c, 0x74, 
0xa1, 0xb8, 0x40 ],

[
0xaf, 0x38, 0x68, 0xc4, 0xc4, 0x70, 0x18, 0xe9, 
0x81, 0x3e, 0x7d, 0x11, 0xb9, 0x9e, 0x75, 0xb0, 
0xa7, 0x99, 0x08, 0x99, 0xff, 0xff, 0x25, 0x8d, 
0xd6, 0xde, 0xd0, 0xf0, 0x56, 0x9c, 0x28, 0x96, 
0xd7, 0xb4, 0x80, 0xfe, 0x38, 0x5a, 0xf5, 0x1e, 
0x1e, 0x1e, 0x9c, 0x2a, 0x1e, 0x57, 0x28 ],

[
0xbf, 0x36, 0x6a, 0x8a, 0xc1, 0x65, 0x40, 0xac, 
0xd4, 0x76, 0x65, 0x0a, 0xec, 0xdf, 0x69, 0xbb, 
0xbb, 0x81, 0x45, 0x85, 0xb0, 0xfc, 0x6c, 0x88, 
0xc1, 0xd4, 0xd0, 0xf0, 0x54, 0x8a, 0x6d, 0x9f, 
0xdc, 0xb9, 0xc6, 0xfc, 0x23, 0x54, 0xb6, 0x14, 
0x12, 0x19, 0x95, 0x6d, 0x57, 0x4c, 0x4c, 0x33, 
0xa6, 0xfb, 0x29, 0xad, 0xb4, 0x92, 0x07, 0xae, 
0xc1, 0xc5, 0xff, 0x12, 0x9f, 0xaa, 0x8c ],

#18
#the top comment on that video is "No penises were harm__________
[
0xaf, 0x3f, 0x62, 0xc4, 0xdc, 0x7e, 0x1c, 0xac, 
0xc3, 0x71, 0x69, 0x13, 0xa9, 0xd0, 0x6a, 0xfe, 
0xa7, 0x80, 0x08, 0x94, 0xf8, 0xeb, 0x71, 0xcc, 
0xd2, 0xd2, 0x94, 0xf4, 0x55, 0xce, 0x24, 0x84, 
0x93, 0xef, 0xee, 0xe6, 0x79, 0x5e, 0xf3, 0x18, 
0x1e, 0x03, 0x9e, 0x79, 0x57, 0x54, 0x47, 0x61, #48
0xa1, 0xb9, 0x22, 0xad, 0xab, 0x9a, 0x10, 0xa3, 
0x4d, 0x1f, 0x60, 0x5c, 0x99, 0xb3, 0xa6, 0xba, 
0x9a, 0x74, 0x24, 0xa6, 0x0c, 0x63, 0x57, 0xfc, 
0x28, 0xa7, 0xbc, 0xa3, 0x29, 0x64, 0x39, 0x85, 
0x7b, 0xff, 0x93, 0x95, 0xcf, 0xa2, 0x73, 0x40, 
0xf1, 0xf2, 0x16, 0x3c, 0xc4, 0x4a, 0x4c, 0x4c, 
0x86, 0xe6, 0x22, 0x17, 0x5b, 0x9c, 0x2d, 0x86, 
0xef, 0xbd, 0xdc, 0x54, 0x51, 0xd9, 0x28, 0x51, 
0x11, 0xbd, 0x58, 0x7c, 0x80, 0xfe, 0x44, 0x5a, 
0x46, 0x12, 0x8b, 0x05, 0x63, 0x95, 0x6f, 0xb7, 
0xae, 0x69, 0x45, 0x23, 0x7b, 0x90, 0xb1, 0x22, 
0x0f, 0xf2, 0x23, 0x36, 0x23, 0x60, 0x1b, 0x1a, 
0x42, 0x5d, 0xef, 0xca, 0xa1, 0x9c, 0x15, 0x07 ],

#19
[
0x8f, 0x3f, 0x62, 0xc4, 0xc3, 0x74, 0x15, 0xac, 
0xc9, 0x6d, 0x3e, 0x5e, 0x9b, 0xd6, 0x67, 0x9a, 
0xa7, 0xa8, 0x49, 0x92, 0xe4, 0xf9, 0x56, 0x81, 
0xc1, 0xd7, 0x9c, 0xae, 0x69, 0x81, 0x19, 0x9f, 
0xd6, 0x89, 0xc5, 0xe8, 0x3f, 0x6d, 0xf7, 0x18, 
0x32, 0x1e, 0x91, 0x65, 0x0e, 0x77, 0x4a, 0x76, 
0xa9, 0xd8, 0x26, 0xbf, 0xb6, 0xd9, 0x7f ],

#20
[0x87, 0x2f, 0x64, 0xd7, 0xa2 ],

#21
[
0xb8, 0x3b, 0x62, 0x92, 0xcd, 0x63, 0x40, 0xac, 
0xd2, 0x77, 0x63, 0x16, 0xb8, 0x81, 0x3e, 0x97, 
0xe8, 0x86, 0x47, 0x90, 0xf5, 0xaa, 0x71, 0x84, 
0xc1, 0x9b, 0x80, 0xf2, 0x5b, 0x9e, 0x6d, 0x80, 
0xdc, 0xbf, 0xcb, 0xec, 0x3d, 0x00, 0x9c ]
]

j = 1
for ciphertext in ciphertexts:
    i = 0
    message = str(j) + ": "
    j = j+1
    for letter in ciphertext:
        if(key[i] != 0):
            decrypted = chr(int(letter ^ key[i])) 
            message += decrypted
        else:
            message += "_"
        i = i+1

    print message

