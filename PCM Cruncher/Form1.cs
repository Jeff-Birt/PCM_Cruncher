using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PCM_Cruncher
{
    public partial class Form1 : Form
    {
        //string inputFileName = "";

        #region UI
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Select file to operate on
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFile_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog1;  
            openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog1.ShowDialog();
            tbFile.Text = openFileDialog1.FileName;
        }

        /// <summary>
        /// Scale 8bit PCM file to taek up entire 0-255 range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnScale_Click(object sender, EventArgs e)
        {
            string inputFile = tbFile.Text;
            string outputFile = "";

            if (File.Exists(inputFile))
            {
                tbStatus.Text += Environment.NewLine;
                tbStatus.Text = "Scaling 8bit PCM 1-255 (_SCL)" + Environment.NewLine;
                int minValue = 0; int maxValue = 0;

                findMinMax(inputFile, true, ref minValue, ref maxValue);
                double scalar = findScalar(minValue, maxValue);
                double offset = -(maxValue - Math.Abs(minValue))/2.0;

                tbStatus.Text += "Original Min: " + minValue.ToString() + Environment.NewLine;
                tbStatus.Text += "Original Max: " + maxValue.ToString() + Environment.NewLine;
                tbStatus.Text += "Offset: " + offset.ToString() + Environment.NewLine;
                tbStatus.Text += "Scalar: " + scalar.ToString() + Environment.NewLine;

                outputFile = scaleFile(inputFile, scalar, offset);
                tbFile.Text = outputFile;   // update file name text box
                
                maxValue = 128; minValue = 128;
                if (File.Exists(outputFile))
                {
                    findMinMax(outputFile, false, ref minValue, ref maxValue);
                }

                tbStatus.Text += "Corrected Min: " + minValue.ToString() + Environment.NewLine;
                tbStatus.Text += "Corrected Max: " + maxValue.ToString() + Environment.NewLine;
                tbStatus.Text += "Done" + Environment.NewLine;
            }
            else
            {
                tbStatus.Text += "Input file does not exist!";
            }
        }  
        
        /// <summary>
        /// Round up and downsample from 8bit to 4bit PCM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCrunch_Click(object sender, EventArgs e)
        {
            string inputFile = tbFile.Text;
            string outputFile = "";
            long outputFileSize = -1;

            if (File.Exists(inputFile))
            {
                outputFile = crunchFile(inputFile);
                tbFile.Text = outputFile;

                if (File.Exists(outputFile))
                {
                    outputFileSize =  new System.IO.FileInfo(outputFile).Length;
                }
                long inputFileSize = new System.IO.FileInfo(inputFile).Length;

                tbStatus.Text += Environment.NewLine;
                tbStatus.Text += "Crunch 8bit PCM to packed 4bit PCM (_CRN)" + Environment.NewLine;
                tbStatus.Text += "Input file size (bytes): " + inputFileSize.ToString() + Environment.NewLine;
                tbStatus.Text += "Ouput file size (bytes): " + outputFileSize.ToString() + Environment.NewLine;
                tbStatus.Text += "Done" + Environment.NewLine;
            }
            else
            {
                tbStatus.Text += "Input file does not exist!";
            }

        }

        /// <summary>
        /// RLE Compress file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCompress_Click(object sender, EventArgs e)
        {
            string inputFile = tbFile.Text;
            string outputFile = "";
            long outputFileSize = -1;

            if (File.Exists(inputFile))
            {
                List<int> value = new List<int>();
                outputFile = rleCompress(inputFile, ref value);
                tbFile.Text = outputFile;   // update file name text box

                if (File.Exists(outputFile))
                {
                    outputFileSize = new System.IO.FileInfo(outputFile).Length;
                }
                long inputFileSize = new System.IO.FileInfo(inputFile).Length;

                tbStatus.Text += Environment.NewLine;
                tbStatus.Text += "RLE compress 4bit packed PCM (_RLE)" + Environment.NewLine;
                tbStatus.Text += "Input file size (bytes): " + inputFileSize.ToString() + Environment.NewLine;
                tbStatus.Text += "Number of nibble clusters: " + value.Count + Environment.NewLine;
                tbStatus.Text += "Nibble clusters size (bytes): " + (value.Count * 2) + Environment.NewLine;
                tbStatus.Text += "Total nibbles in clusters: " + value.Sum() + Environment.NewLine;
                tbStatus.Text += "Total bytes in clusters: " + value.Sum() / 2 + Environment.NewLine;
                tbStatus.Text += "Output file size (bytes): " + outputFileSize.ToString() + Environment.NewLine;
                tbStatus.Text += "Done" + Environment.NewLine;
            }
            else
            {
                tbStatus.Text += "Input file does not exist!";
            }
        }

        /// <summary>
        /// Decompress RLE encoded file, used as a test
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDecompress_Click(object sender, EventArgs e)
        {
            string inputFile = tbFile.Text;
            string outputFile = "";
            long outputFileSize = -1;

            if (File.Exists(inputFile))
            {
                outputFile = rleDecompress(inputFile);
                tbFile.Text = outputFile;   // update file name text box

                if (File.Exists(outputFile))
                {
                    outputFileSize = new System.IO.FileInfo(outputFile).Length;
                }
                long inputFileSize = new System.IO.FileInfo(inputFile).Length;

                tbStatus.Text += Environment.NewLine;
                tbStatus.Text += "RLE decompress 4bit packed PCM (_ELR)" + Environment.NewLine;
                tbStatus.Text += "Input file size (bytes): " + inputFileSize.ToString() + Environment.NewLine;
                tbStatus.Text += "Output file size (bytes): " + outputFileSize.ToString() + Environment.NewLine;
                tbStatus.Text += "Done" + Environment.NewLine;
            }
            else
            {
                tbStatus.Text += "Input file does not exist!";
            }
        }

        #endregion UI

        /// <summary>
        /// Find minimum and maximum byte values in abinary file
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        private void findMinMax(string inputFile, bool signed, ref int minValue, ref int maxValue)
        {
            if (inputFile != "")
            {
                FileStream inputfs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                long fileSize = inputfs.Length;
                for (long i = 0; i < fileSize; i++)
                {
                    int v1;
                    if (signed)
                    {
                        v1 = fileReader.ReadSByte();
                    }
                    else
                    {
                        v1 = fileReader.ReadByte();
                    }

                    if (v1 < minValue)
                    {
                        minValue = v1;
                    }
                    else if (v1 > maxValue)
                    {
                        maxValue = v1;
                    }
                }

                fileReader.Close();
                inputfs.Close();
            }
        }

        /// <summary>
        /// Finds scalar needed to scale byte array to 0-255
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        private double findScalar(int minValue, int maxValue)
        {
            double span = Math.Abs(minValue) + maxValue;

            return 255/span;
        }

        /// <summary>
        /// Scales binary file to byte range 0-255
        /// </summary>
        /// <param name="inputFile"></param>
        private string scaleFile(string inputFile, double scalar, double offset)
        {
            string outputFile = ""; // will return empty string if input filename empty

            if (inputFile != "")
            {
                outputFile = buildOutputFileName(inputFile, "_SCL");

                FileStream inputfs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                long fileSize = inputfs.Length;

                for (long i = 0; i < fileSize; i++)
                {
                    int v1 = (int)(((double)fileReader.ReadSByte() + offset) * scalar) + 128;
                    v1 = Math.Min(255, v1); // keep from going over
                    v1 = Math.Max(0, v1);   // or under range

                    fileWriter.Write((byte)v1);
                }

                fileReader.Close();
                inputfs.Close();

                fileWriter.Close();
                outputfs.Close();
            }
            return outputFile;
        }

        /// <summary>
        /// Rounds a scaled file and downsample to 4bits, cruches two 4bit samples
        /// in one output byte
        /// </summary>
        /// <param name="inputFile"></param>
        /// <returns>output file name</returns>
        private string crunchFile(string inputFile)
        {
            string outputFile = "";

            if (inputFile != "")
            {
                outputFile = buildOutputFileName(inputFile, "_CRN");

                FileStream inputfs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                int lowNibble = 0;

                long fileSize = inputfs.Length;
                for (long i = 0; i < fileSize; i++)
                {
                    byte nextByte = fileReader.ReadByte();
                    if (nextByte + 2 < 0xFF) { nextByte += 2; } // round up lower nibble

                    // If this is an odd byte save upper nibble shifted to lower nibble position
                    // If this is an even byte combine this upper nibble with last nibble
                    if ((i % 2) == 0)
                    {
                        int highNibble = (int)nextByte & 0xF0;
                        lowNibble = lowNibble | highNibble;
                        lowNibble = Math.Max(1, lowNibble); // make sure we have no 0x00 bytes
                        fileWriter.Write((byte)lowNibble);
                    }
                    else
                    {
                        lowNibble = nextByte >> 4;
                    }
                }

                fileReader.Close();
                inputfs.Close();

                fileWriter.Close();
                outputfs.Close();

            }

            return outputFile;
        }

        /// <summary>
        /// RLE Compress a 4bit packed sample file
        /// </summary>
        /// <param name="inputFile"></param>
        /// <returns></returns>
        private string rleCompress(string inputFile, ref List<int> value)
        {
            string outputFile = "";

            int lastNibble = -1; int lowNibble = -1; int highNibble = -1; int count = 1;
            byte nextByte;

            if (inputFile != "")
            {
                outputFile = buildOutputFileName(inputFile, "_RLE");

                FileStream inputfs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                long fileSize = inputfs.Length;
                for (long i = 0; i < fileSize; i++)
                {
                    // if both nibbles are not -1 here we goofed and did not processes them last loop
                    //if (lowNibble != -1 | highNibble != -1) {tbStatus.Text += "Error!";}

                      nextByte = fileReader.ReadByte();
                     lowNibble = (int)nextByte & 0x0F;
                    highNibble = (int)nextByte >> 4;

                    // if lowNibble matches previous nibble (lastNibble) 
                    if ( (lowNibble == lastNibble) & (count < 15) )
                    {
                        count++; // keep track of the number of matching nibbles

                        // if we have a full set of nibbles, write out RLE byte pair
                        if (count == 15) 
                        {
                            value.Add(count);           // track stats
                            writeRLEPair(fileWriter, ref count, ref lastNibble, ref highNibble);
                        }
                        lowNibble = -1;                 // low nibble was consumed
                    }

                    // if lowNibble not consumed above keep processing it
                    if (lowNibble != -1)
                    {
                        // Are we are on a 'new' pass.
                        if ( (count == 1) & (lastNibble == -1) )
                        {
                            lastNibble = lowNibble;     // Promote lastNibble for highNibble testing below
                            lowNibble = -1;             // consumed it flag
                        }
                        else if (count > 5)
                        {
                            // if #nibbles >5 enough to REL
                            int oldCount = count;
                            writeRLEPair(fileWriter, ref count, ref lastNibble, ref lowNibble);
                            value.Add(oldCount - count);  // count only nibbles in pairs
                        }
                        else // count >= 1 and count < 5
                        {
                            // if count > 1 we did not have enough nibbles for an RLE pair 
                            // write out as many lastNibble pairs as possible
                            while (count > 1)
                            {
                                fileWriter.Write((byte)((lastNibble << 4) | lastNibble));
                                count -= 2;
                            }

                            // if count = 1 we have an odd # of nibbles, one will be left over from above 
                            // or we could be on new pass where a nibble is left in lastNibble that != lowNibble
                            if (count == 1)
                            {
                                fileWriter.Write((byte)((lowNibble << 4) | lastNibble));

                                lastNibble = highNibble;    // bypass highNibble testing below
                                highNibble = -1;            // as we are out of nibbles
                                lowNibble = -1;             // consumed it flag
                                count = 1;                  // and need it to start a 'new' pass
                            }
                            else // *** not sure about this maybe instrument to see if it gets called **
                            {
                                // we still have the lowNibble
                                lastNibble = lowNibble; // promote to use for highNibble test below
                                lowNibble = -1;     // used it flag
                                count = 1;
                            }
                        } // end of else count > 1 and count < 5
                    } // end of lowNibble != -1

                    // see if we have a highNibble left (not used above)
                    if (highNibble != -1)
                    {
                        // if highNibble matches previous nibble (lastNibble)
                        if ( (highNibble == lastNibble) & (count < 15) )
                        {
                            count++;

                            // if we have a full set of nibbles write out RLE pair
                            // lowNibble is being used as placeholder, it is already == -1
                            if (count == 15)
                            {
                                value.Add(count);           // track stats
                                writeRLEPair(fileWriter, ref count, ref lastNibble, ref lowNibble);
                            }
                            highNibble = -1;        // highNibble was consumed
                        }

                        // Is highNibble still left, if so keep processing
                        if (highNibble != -1)
                        {
                            // highNibble != lastNibble and count == 1 write out byte
                            if (count == 1)
                            {
                                fileWriter.Write((byte)((highNibble << 4) | lastNibble));

                                lastNibble = -1;    // consumed it flag, flags new pass
                                highNibble = -1;    // consumed it flag
                                count = 1;
                            }
                            else if (count > 5)
                            {
                                // nibbles >5 enough to write out RLE pair
                                int oldCount = count;
                                writeRLEPair(fileWriter, ref count, ref lastNibble, ref highNibble);
                                value.Add(oldCount - count); // count only nibbles in pairs
                            }
                            else // count > 1 and count < 5
                            {
                                // we did not have enough nibbles for an RLE pair 
                                // we have #n nibbles of lastNibble to write
                                // make byte of two lastNibble, write out count / 2 byt
                                while (count > 1)
                                {
                                    fileWriter.Write((byte)((lastNibble << 4) | lastNibble));
                                    count -= 2;
                                }

                                // if odd number of nibbles one will be left over from above 
                                // make nibble of lastNibble | highNibble, write it out
                                if (count == 1)
                                {
                                    fileWriter.Write((byte)((highNibble << 4) | lastNibble));

                                    lastNibble = -1;     // we have used all nibble, flag new pass
                                    highNibble = -1;     // flag it used
                                    count = 1;
                                }
                                else
                                {
                                    // we still have the highNibble left so promote it to lastNibble
                                    lastNibble = highNibble;
                                    highNibble = -1;     // used it flag
                                    count = 1;
                                }
                            } // end of else // count > 1 and count < 5

                        } // end of (highNibble != -1) test #2
                    } // end of (highNibble != -1) test #1

                } // end of file processing

                // *** need to handle a left over nibble or byte here
                if (lastNibble != -1)
                {
                    //tbStatus.Text += "count: " + count.ToString() + Environment.NewLine;
                    //tbStatus.Text += "lastNibble: " + lastNibble.ToString() + Environment.NewLine;
                    //tbStatus.Text += " lowNibble: " + lowNibble.ToString() + Environment.NewLine;
                    //tbStatus.Text += "highNibble: " + highNibble.ToString() + Environment.NewLine;

                    fileWriter.Write((byte)((lastNibble << 4) | lastNibble));

                    // if count > 1 we did not have enough nibbles for an RLE pair 
                    // make byte of two last nibbles, write out count / 2 bytes
                    while (count > 2)
                    {
                        fileWriter.Write((byte)((lastNibble << 4) | lastNibble));
                        count -= 2;
                    }
                    //tbStatus.Text += "final count: " + count.ToString() + Environment.NewLine;
                }

                fileReader.Close();
                inputfs.Close();

                fileWriter.Close();
                outputfs.Close();
            }

            return outputFile;
        }

        /// <summary>
        /// Uncompress an RLE compressed 4bit packed file
        /// </summary>
        /// <param name="inputFile"></param>
        private string rleDecompress(string inputFile)
        {
            string outputFile = "";

            List<int> value = new List<int>();
            int lastNibble = -1; int lowNibble = -1; int highNibble = -1; //int count = 1;
            byte nextByte; int temp;

            if (inputFile != "")
            {
                outputFile = buildOutputFileName(inputFile, "_ELR");

                FileStream inputfs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                long fileSize = inputfs.Length;
                for (long i = 0; i < fileSize; i++)
                {
                    if (lowNibble != -1 | highNibble != -1)
                    {
                        tbStatus.Text += "Error";
                    }

                    nextByte = nextByte = fileReader.ReadByte();
                    lowNibble = (int)nextByte & 0x0F; // low nibble
                    highNibble = (int)nextByte >> 4;   // high nibble

                    // if nextByte is 0x00 then the next byte is an RLE byte
                    if (nextByte == 0x00)
                    {
                        i++; // make sure index stays in sync
                        // read next byte in as it is an RLE byte
                        nextByte = nextByte = fileReader.ReadByte();
                        lowNibble = (int)nextByte & 0x0F; // low nibble
                        highNibble = (int)nextByte >> 4;   // high nibble

                        // We need to write out #highNibble values of lowNibble 
                        // if we have a left over nibble we need to consume it first
                        if (lastNibble != -1)
                        {
                            temp =  (lowNibble << 4) | lastNibble;
                            fileWriter.Write((byte)temp); // 

                            highNibble -= 1;    // account for nibble we used
                            lastNibble = -1;    // flag as used
                        }

                        // now write out remaining nibbles packed in bytes
                        temp = (lowNibble << 4) | lowNibble;
                        for (int j = 0; j < highNibble / 2; j++)
                        {
                            fileWriter.Write((byte)temp); // #nibbes|nibble_value
                        }
                        lastNibble = -1;    // flag as used

                        // If a nibble is left over save it as lastNibble
                        if (highNibble % 2 != 0)
                        {
                            lastNibble = lowNibble;
                        }

                        lowNibble = -1;     // flag as used
                        highNibble = -1;    // flag as used
                    }
                    else
                    {
                        // nextByte was not RLE, just ordinary nibbles

                        // we have no left over nibble so write out nextByte directly
                        if (lastNibble == -1)
                        {

                            fileWriter.Write((byte)nextByte);
                            lastNibble = -1;    // flag as used
                            lowNibble = -1;     // flag as used
                            highNibble = -1;    // flag as used
                        }
                        else
                        {
                            // we have a nibble left over we need to consume
                            temp = (lowNibble << 4) | lastNibble;
                            fileWriter.Write((byte)temp); // 

                            lastNibble = highNibble; // now save left over highNibble
                            lowNibble = -1;     // flag as used
                            highNibble = -1;    // flag as used
                        }
                    }

                }

                fileReader.Close();
                inputfs.Close();

                fileWriter.Close();
                outputfs.Close();
            }

            return outputFile;
        }

        /// <summary>
        /// Helper to write out an RLE 'pair',
        /// a byte where high nibble is # of encoded nibbles, and low nibble is value of encoded nibbles
        /// </summary>
        /// <param name="fileWriter"></param>
        /// <param name="count"></param>
        /// <param name="lastNibble"></param>
        /// <param name="sourceNibble"></param>
        private void writeRLEPair(BinaryWriter fileWriter, ref int count, ref int lastNibble, ref int sourceNibble)
        {
            fileWriter.Write((byte)0x00); // RLE flag
            fileWriter.Write((byte)((count << 4) | lastNibble)); // #nibbes|nibble_value

            lastNibble = sourceNibble;    // consumed lastNibble replace with sourceNibble
            sourceNibble = -1;            // flag sourceNibble consumed
            count = 1;                    // reset count
        }

        /// <summary>
        /// Helper to build output file name
        /// </summary>
        /// <param name="inputFile"></param>
        /// <returns></returns>
        private string buildOutputFileName(string inputFile, string fileType)
        {
            return  Path.Combine(Path.GetDirectoryName(inputFile),
                    Path.GetFileNameWithoutExtension(inputFile) + fileType +
                    Path.GetExtension(inputFile));
        }


    }
}
