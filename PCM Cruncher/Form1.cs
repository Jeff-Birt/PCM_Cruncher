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
        string inputFileName = "";

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
            inputFileName = openFileDialog1.FileName;
            //outputFileName = Path.Combine(Path.GetDirectoryName(inputFileName),
            //                          Path.GetFileNameWithoutExtension(inputFileName) + "_Crunched8" +
            //                          Path.GetExtension(inputFileName) );
        }

        /// <summary>
        /// Round up and downsample from 8bit to 4bit PCM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCrunch_Click(object sender, EventArgs e)
        {
            if (inputFileName != "")
            {            
                tbStats.Text = "Processing";

                string outputFile = crunchFile(inputFileName);

                tbStats.Text = "Done";
            }
        }

        /// <summary>
        /// Scale 8bit PCM file to taek up entire 0-255 range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnScale_Click(object sender, EventArgs e)
        {
            string outputFile = "";

            if (inputFileName != "")
            {
                tbStats.Text = "Processing" + Environment.NewLine;

                int minValue = 0;
                int maxValue = 0;

                findMinMax(inputFileName, true, ref minValue, ref maxValue);
                double scalar = findScalar(minValue, maxValue);

                tbStats.Text += Environment.NewLine;
                tbStats.Text += "Min: " + minValue.ToString() + Environment.NewLine;
                tbStats.Text += "Max: " + maxValue.ToString() + Environment.NewLine;
                tbStats.Text += "Scalar: " + scalar.ToString() + Environment.NewLine;

                outputFile = scaleFile(inputFileName, scalar);
                minValue = 128;
                maxValue = 128;
                findMinMax(outputFile, false, ref minValue, ref maxValue);

                tbStats.Text += "Corrected Min: " + minValue.ToString() + Environment.NewLine;
                tbStats.Text += "Corrected Max: " + maxValue.ToString() + Environment.NewLine;
                tbStats.Text += "Corrected Scalar: " + scalar.ToString() + Environment.NewLine;

                tbStats.Text += Environment.NewLine + "Done";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAnalize_Click(object sender, EventArgs e)
        {
            List<int> value = rleCompress(inputFileName);
            FileStream inputfs = new FileStream(inputFileName, FileMode.Open, FileAccess.Read);
            long fileSize = inputfs.Length;
            inputfs.Close();

            tbStats.Text += Environment.NewLine + "File REL Analysis" + Environment.NewLine;
            tbStats.Text += "Number of nibble clusters: " + value.Count + Environment.NewLine;
            tbStats.Text += "Total nibbles in clusters: " + value.Sum() + Environment.NewLine;
            tbStats.Text += "File size (bytes): " + fileSize.ToString() + Environment.NewLine;

            // REL file size fileSize - sum + count * 2
            //long relFileSize = fileSize - value.Sum() + (value.Count * 2);
            //tbStats.Text += "REL encode file size: " + relFileSize.ToString() + Environment.NewLine;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDecompress_Click(object sender, EventArgs e)
        {
            rleDecompress(inputFileName);
        }


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
            double scalar = 0;
            if (Math.Abs(minValue) > Math.Abs(maxValue))
            {
                scalar = 1 - Math.Abs(minValue) / 128.0;
            }
            else
            {
                scalar = 1 - Math.Abs(maxValue) / 128.0;
            }
            scalar += 1;

            return scalar;
        }

        /// <summary>
        /// Scakes binary file to byte range 0-255
        /// </summary>
        /// <param name="inputFile"></param>
        private string scaleFile(string inputFile, double scalar)
        {
            string outputFile = ""; // will return empty string if input filename empty

            if (inputFile != "")
            {
                outputFile = Path.Combine(Path.GetDirectoryName(inputFile),
                             Path.GetFileNameWithoutExtension(inputFile) + "_S" +
                             Path.GetExtension(inputFile));

                FileStream inputfs = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                long fileSize = inputfs.Length;

                for (long i = 0; i < fileSize; i++)
                {
                    int v1 = (int)((double)fileReader.ReadSByte() * scalar) + 128;
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

            if (inputFileName != "")
            {
                outputFile = Path.Combine(Path.GetDirectoryName(inputFileName),
                             Path.GetFileNameWithoutExtension(inputFileName) + "_RC" +
                             Path.GetExtension(inputFileName));

                FileStream inputfs = new FileStream(inputFileName, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                int lowNibble = 0;

                long fileSize = inputfs.Length;
                for (long i = 0; i < fileSize; i++)
                {
                    byte nextByte = fileReader.ReadByte();
                    if (nextByte + 2 < 0xFF) { nextByte += 2; } // round up lower nibble

                    // on odd bytes save upper nibble shifted to lower nibble position
                    // on even bytes combine upper nibble of even with odd byte
                    if ((i % 2) == 0) 
                    {
                        int highNibble = (int)nextByte & 0xF0;
                        lowNibble = lowNibble | highNibble;
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
        private List<int> rleCompress(string inputFile)
        {
            string outputFile = "";

            List<int> value = new List<int>();
            int lastNibble = -1; int lowNibble = -1; int highNibble = -1; int count = 1;
            byte nextByte;

            if (inputFileName != "")
            {
                outputFile = buildOutputFileName(inputFileName, "_RLE");

                FileStream inputfs = new FileStream(inputFileName, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                long fileSize = inputfs.Length;
                for (long i = 0; i < fileSize; i++)
                {
                    // if both nibbles are not -1 here we goofed and did not processes them last loop
                    if (lowNibble != -1 | highNibble != -1) {tbStats.Text += "Error!";}

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
                            fileWriter.Write((byte)0x00); // RLE flag
                            fileWriter.Write((byte)((count << 4) | lastNibble)); // #nibbes|nibble_value

                            lastNibble = highNibble;    // skip highNibble test below as we
                            highNibble = -1;            // have the high nibble left over
                            count = 1;                  // and need it to start a 'new' pass
                        }

                        lowNibble = -1;                 // low nibble was consumed
                    }

                    // if lowNibble not consumed above check if we should write it out
                    if (lowNibble != -1)
                    {
                        // Are we are on a 'new' pass.
                        if ( (count == 1) & (lastNibble == -1) )
                        {
                            lastNibble = lowNibble; // Promote lastNibble for highNibble test below
                            lowNibble = -1;         // consumed it flag
                        }
                        else if (count > 5)
                        {
                            // if #nibbles >5 enough to REL
                            value.Add(count);

                            fileWriter.Write((byte)0x00); // RLE flag
                            fileWriter.Write((byte)((count << 4) | lastNibble)); // #nibbes|nibble_value

                            lastNibble = lowNibble;    // skip highNibble test below as we
                            lowNibble = -1;            // have the high nibble left over
                            count = 1;                 // and need it to start a 'new' pass
                        }
                        else // count >= 1 and count < 5
                        {
                            // if count > 1 we did not have enough nibbles for an RLE pair 
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
                            if (count == 15)
                            {
                                fileWriter.Write((byte)0x00); // RLE flag
                                fileWriter.Write((byte)((count << 4) | lastNibble)); // #nibbes|nibble_value

                                lastNibble = -1;    // we have consumed lastNibble
                                count = 1;          // and need it to start a 'new' pass
                            }
                            highNibble = -1;        // highNibble was consumed
                        }

                        // do we stil have teh highNibble left?
                        if (highNibble != -1)
                        {
                            // highNibble != lastNibble and count == 1 write out byte
                            if (count == 1)
                            {
                                fileWriter.Write((byte)((highNibble << 4) | lastNibble));

                                lastNibble = -1;    // tag as used, flag new pass
                                highNibble = -1;    // tag as used
                                count = 1;
                            }
                            else if (count > 5)
                            {
                                // nibbles >5 enough to write out RLE pair
                                value.Add(count);

                                fileWriter.Write((byte)0x00); // RLE flag
                                fileWriter.Write((byte)((count << 4) | lastNibble)); // #nibbes|nibble_value

                                lastNibble = highNibble;    // promote highNibble to LastNibble
                                highNibble = -1;            // we have consumed highNibble
                                count = 1;
                            }
                            else // count > 1 and count < 5
                            {
                                // we did not have enough nibbles for an RLE pair 
                                // we have #n nibbles of lastNibble to write
                                // make byte of two last nibbles, write out count / 2 byt
                                for (int j = 0; j < count / 2; j++)
                                {
                                    fileWriter.Write((byte)((lastNibble << 4) | lastNibble));
                                }

                                // if odd number of nibbles one will be left over from above 
                                // make nibble of lastNibble | highNibble, write it out
                                if (count % 2 != 0)
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
                // *** check count and crate byte of lastNibble << 4 | lastNibble
                // *** not sure what to do if only a nibble is left over
                if (lastNibble != -1)
                {
                    tbStats.Text += "count: " + count.ToString() + Environment.NewLine;
                    tbStats.Text += "lastNibble: " + lastNibble.ToString() + Environment.NewLine;
                    tbStats.Text += " lowNibble: " + lowNibble.ToString() + Environment.NewLine;
                    tbStats.Text += "highNibble: " + highNibble.ToString() + Environment.NewLine;

                    fileWriter.Write((byte)((lastNibble << 4) | lastNibble));

                    // if count > 1 we did not have enough nibbles for an RLE pair 
                    // make byte of two last nibbles, write out count / 2 bytes
                    while (count > 2)
                    {
                        fileWriter.Write((byte)((lastNibble << 4) | lastNibble));
                        count -= 2;
                    }
                    tbStats.Text += "final count: " + count.ToString() + Environment.NewLine;

                }

                fileReader.Close();
                inputfs.Close();

                fileWriter.Close();
                outputfs.Close();
            }

            return value;
        }

        /// <summary>
        /// Uncompress an RLE compressed 4bit packed file
        /// </summary>
        /// <param name="inputFile"></param>
        private void rleDecompress(string inputFile)
        {
            string outputFile = "";

            List<int> value = new List<int>();
            int lastNibble = -1; int lowNibble = -1; int highNibble = -1; int count = 1;
            byte nextByte; int temp;

            if (inputFileName != "")
            {
                outputFile = Path.Combine(Path.GetDirectoryName(inputFileName),
                             Path.GetFileNameWithoutExtension(inputFileName) + "_ELR" +
                             Path.GetExtension(inputFileName));

                FileStream inputfs = new FileStream(inputFileName, FileMode.Open, FileAccess.Read);
                BinaryReader fileReader = new BinaryReader(inputfs);

                FileStream outputfs = new FileStream(outputFile, FileMode.CreateNew);
                BinaryWriter fileWriter = new BinaryWriter(outputfs);

                long fileSize = inputfs.Length;
                for (long i = 0; i < fileSize; i++)
                {
                    if (lowNibble != -1 | highNibble != -1)
                    {
                        tbStats.Text += "Error";
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


            }

        }

        /// <summary>
        /// Helper to build output file name
        /// </summary>
        /// <param name="inputFile"></param>
        /// <returns></returns>
        private string buildOutputFileName(string inputFile, string fileType)
        {
            return  Path.Combine(Path.GetDirectoryName(inputFileName),
                    Path.GetFileNameWithoutExtension(inputFileName) + fileType +
                    Path.GetExtension(inputFileName));
        }





    }
}
