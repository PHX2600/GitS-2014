/* vimzipper.c - read in a vim-encrypted file and wrap it as an encrypted
 *               zip archive so that it can be directly attacked using the
 *               widely-known zip encryption breakers like PKcrack.
 *
 * Richard Jones
 * December 26, 2006
 *
 * Background
 * ----------
 *  This tool was written to facilitate the cracking of text files that
 *  have been encrypted using the "vim -x" facility of the popular vim
 *  editor.  Such files will be known as "vim-x" files henceforth in this
 *  file.  The encryption used by vim-x (as of v6.1) is identical to
 *  that of PKZIP (and InfoZip, NetZip, WinZip, etc.) so they can be
 *  decrypted without knowing the encryption password using the well-known
 *  plain text attack first described by Eli Biham and Paul Kocher in 
 *
 *  ``A Known Plaintext Attack on the PKZIP Stream Cipher'',
 *    Fast Software Encryption 2, Proceedings of the Leuven Workshop,
 *    LNCS 1008, December 1994.
 *
 *  The PKZIP standard encryption scheme has continued to be broadly used,
 *  in spite of its weak security, because of it was able to evade the strict
 *  U.S. export controls on encryption software that were in place in the
 *  1990's.  It was also favored because of its efficiency and its widely
 *  published implementations.  The original "vi" editor (and its command
 *  line equivalent ex) on solaris had a -x argument which enabled users
 *  to encrypt disk images of text created in the editor with a password,
 *  based upon the unix "crypt" clib function.  The authors of the vim
 *  "vi improved" editor for Linux/Windows (see web site http://www.vim.org)
 *  implemented a similar functionality, but based it on the PKZIP encryption
 *  algorithm instead of crypt.  By the mid-1980's a text file encoded using
 *  crypt could be broken using automatic brute-force methods within a matter
 *  of hours on a desktop PC.  Unfortunately, within the following decade
 *  the PKZIP algorithm had suffered a similar fate.  However, the makers of
 *  vim stuck with the zip algorithm, probably for compatibility reasons and
 *  to avoid legal export restrictions associated with strong encryption
 *  algorithms.  As a result, those of us who like the convenience of "vi -x"
 *  are stuck with managing its weaknesses and making sure our most sensitive
 *  data are protected in other ways.
 *
 * Method:
 * -------
 *  The attack described by Biham and Kocher relies on the user having access
 *  to a fragment of the decrypted file and knowing its offset in the file
 *  byte stream.  This is not very useful if the attacker has no other way
 *  to guess the contents of a file than to look at the filename and size
 *  of the encrypted file.  However, in an archive containing many files, there
 *  may be one for which an unencrypted copy can be found elsewhere.  Once
 *  that is done, the archive encryption is all but broken.  A number of
 *  programs implementing plain-text attacks on various types of zip files
 *  are widely available for download.  A very good one is PKcrack by Peter
 *  Conrad.
 *
 *  PKcrack can work either on files inside zip archives or on the extracted
 *  contents, and requires only the first 13 bytes of one file in order to
 *  uniquely find the encryption key.  The key is a set of 3 4-byte integers
 *  that completely contains the internal state of the encryption engine at
 *  the start of the file.  Normally it is generated by the zip utility from
 *  a password chosen by the user, but it is the key itself that is needed to
 *  decrypt the archive, not the password.  In cases where the user has chosen
 *  a very complex password, PKcrack can quickly report the initial keys.  It
 *  also supports a reverse search for the password, but this is a brute-force
 *  attack and not very effective against good passwords.  With the key only,
 *  the entire zip file can be instantly decrypted with no errors.  The point
 *  of vimzipper is to wrap a vim-x file within a zip archive so that the
 *  PKcrack tools can be used to decipher it.
 *
 * Differences between vim-x files and zip archives
 * -------------------------------------------------
 *  Commonly the zip utility compresses text files before it stores them in
 *  an archive.  This should be a good defence against plain-text attacks
 *  because it makes the contents of the first few bytes of a file dependent
 *  on the contents of the entire file.  However, the PKZIP encryption rules
 *  state that a 12-byte random string must be prepended to the encrypted data
 *  stream, and it seems that zip utility software authors have not been very
 *  careful about making it random.  In 2001 Michael Stay (staym@accessdata.com)
 *  published a note reporting that the 12-byte string prepended by the
 *  popular WinZip program is highly predictable.  Furthermore, the encryption
 *  engine is initialized afresh (with the same password) at the beginning of
 *  each archived file, so that combining these 12 characters with the header
 *  of any structured file can serve as the known plain-text for a plain-text
 *  attack.  Using this knowledge it is possible to crack a zip archive that
 *  contains a typical user's home directory within a couple of hours on a
 *  desktop PC.
 *
 *  These efficient methods are not effective against most most vim-x files
 *  because (so far) there is no generally effective strategy for predicting
 *  the contents of a text file created in an editor.  However, vim-x files
 *  are not compressed prior to encryption, so plain-text attacks using a
 *  short file fragment are still effective.  This means that trying a random
 *  selection of common phrases (or computer language keywords for source
 *  files) placed at random offsets within the file has a good chance of
 *  success within a relatively short period of time.  The basic weakness of
 *  PKZIP is that the key space is not very large, a fact that remains no
 *  matter how long a password is chosen.
 *
 * Usage:
 * ------
 *  Mounting an attack on a vim-x file requires two steps.
 *  1. Use vimzipper to transform the vim-x file into a zip archive.
 *     The output zip file from vimzipper is not fully zip-compatible, but
 *     it can be scanned by zipinfo and analyzed by pkcrack and other zip
 *     file decryption tools.
 *  2. Run pkcrack in the usual way on the zip file to extract the keys.
 *  3. Run zipdecrypt on the zip file to create the decrypted output file
 *     in the form of a zip file like the original one, without encryption.
 */

#include <stdlib.h>
#include <stdio.h>

int main(int argc, char* argv[])
{
   int arg;
   char *command;
   char *p1,*p2;
   FILE* zipfd;
   int done;

   if ((argc < 3) || (argc > 999)) {
      fprintf(stderr,"\n Usage: vimzipper <outfile.zip> <vim-x-file> ... \n\n");
      exit(1);
   }

   // issue the zip command to create the archive

   p1 = command = malloc(80);
   p2 = p1+80;
   for (arg=0; arg < argc; ++arg) {
      if (p1+strlen(argv[arg])+1 >= p2) {
         int size = p2-command;
         p2 = malloc(size+80);
         strncpy(p2,command,size);
         p1 += p2-command;
         free(command);
         command = p2;
         p2 += size+80;
      }
      if (arg == 0) {
         char zipper[] = "zip";
         snprintf(p1,strlen(zipper)+1,"%s",zipper);
         p1 += strlen(zipper);
      }
      else {
         strncpy(p1,argv[arg],strlen(argv[arg])+1);
         p1 += strlen(argv[arg]);
      }
      strncpy(p1," ",2);
      ++p1;
   }
   unlink(argv[1]);
   if (system(command) != 0) {
      fprintf(stderr,"Error: zip command returned failure code, giving up!\n");
      exit(1);
   }

   // go through and mark all vim-x files as zip-encrypted

   zipfd = fopen(argv[1],"r+");
   if (zipfd == 0) {
      fprintf(stderr,"Error: unable to create output zip archive %s!\n",
              argv[1]);
      exit(1);
   }
   done = 0;
   while (done == 0) {
      int16_t sign[4];
      struct zip_hdr_t {
         int16_t gpb;
         int16_t comp;
         int16_t time;
         int16_t date;
         int32_t crc;
         int32_t csize;
         int32_t uncsize;
         int16_t flen;
         int16_t extralen;
      } hdr;
      int extras[32];
      FILE *fd;
      char filename[999];
      char magic[15];
      long int pos_hdr;
      int count = fread(&sign,sizeof(int16_t),3,zipfd);
      if (sign[0] != 0x4b50) {
         fprintf(stderr,"Error: unrecognized header %x found in archive!\n",
                 sign[1]);
         exit(1);
      }
      switch (sign[1]) {
       case 0x0201:
         count = fread(&sign[3],sizeof(int16_t),1,zipfd);
         pos_hdr = ftell(zipfd);
         count += fread(&hdr,sizeof(struct zip_hdr_t),1,zipfd);
         count += fread(&extras,14,1,zipfd);
         count += fread(&filename,hdr.flen,1,zipfd);
         break;
       case 0x0403:
         pos_hdr = ftell(zipfd);
         count = fread(&hdr,sizeof(struct zip_hdr_t),1,zipfd);
         count += fread(&filename,hdr.flen,1,zipfd);
         break;
       case 0x0605:
         done = 1;
         continue;
       default:
         fprintf(stderr,"Error: unrecognized header %x found in archive!\n",
                 sign[1]);
         exit(1);
      }
      filename[hdr.flen] = '\0';
      if ((fd = fopen(filename,"r")) == 0) {
         fprintf(stderr,"Error: input file %s cannot be openned!\n",
                 filename);
         exit(1);
      }
      else {
         int bc = fread(&magic,1,12,fd);
         if ((bc == 12) && (strncmp(magic,"VimCrypt~01!",12) == 0)) {
            hdr.gpb |= 1;
            hdr.uncsize -= 12;
            fseek(zipfd,pos_hdr,SEEK_SET);
            bc = fwrite(&hdr,sizeof(struct zip_hdr_t),1,zipfd);
            if (sign[1] == 0x0403) {
               printf("  touching up %s header (vim encrypted)\n",filename);
            }
            else if (sign[1] == 0x0201) {
               printf("  touching up %s trailer (vim encrypted)\n",filename);
            }
         }
         fclose(fd);
      }
      pos_hdr += sizeof(struct zip_hdr_t);
      pos_hdr += hdr.flen;
      pos_hdr += hdr.extralen;
      switch (sign[1]) {
       case 0x0201:
         pos_hdr += 14;
         break;
       case 0x0403:
         pos_hdr += hdr.csize;
      }
      fseek(zipfd,pos_hdr,SEEK_SET);
   }
   fclose(zipfd);
}
