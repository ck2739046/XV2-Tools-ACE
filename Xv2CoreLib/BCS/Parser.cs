﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.BCS
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        List<byte> bytes;
        public BCS_File bcsFile { get; private set; } = new BCS_File();


        public Parser(string location, bool _writeXml = false)
        {
            saveLocation = location;
            rawBytes = File.ReadAllBytes(location);
            bytes = rawBytes.ToList();
            Validation();
            Parse();
            if (_writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(BCS_File));
                serializer.SerializeToFile(bcsFile, saveLocation + ".xml");
            }
        }

        public Parser(byte[] _bytes)
        {
            rawBytes = _bytes;
            bytes = rawBytes.ToList();
            Validation();
            Parse();
        }

        public BCS_File GetBcsFile()
        {
            return bcsFile;
        }

        private void Validation()
        {
            if(BitConverter.ToInt16(rawBytes, 6) == 72)
            {
                Console.WriteLine("Xenoverse 1 BCS not supported.");
                Utils.WaitForInputThenQuit();
            }
        }

        private void Parse()
        {
            //counts
            int partsetCount = BitConverter.ToInt16(rawBytes, 12);
            int partcolorsCount = BitConverter.ToInt16(rawBytes, 14);
            int bodyCount = BitConverter.ToInt16(rawBytes, 16);

            //offsets
            int partsetOffset = BitConverter.ToInt32(rawBytes, 24);
            int partcolorsOffset = BitConverter.ToInt32(rawBytes, 28);
            int bodyOffset = BitConverter.ToInt32(rawBytes, 32);
            int skeleton2Offset = BitConverter.ToInt32(rawBytes, 36);
            int skeleton1Offset = BitConverter.ToInt32(rawBytes, 40);

            //Header
            bcsFile.I_44 = BitConverter.ToInt32(rawBytes, 44);
            bcsFile.F_48 = BitConverter_Ex.ToFloat32Array(rawBytes, 48, 7);

            //PartSets
            int actualIndex = 0;
            if(partsetCount > 0)
            {
                bcsFile.PartSets = new List<PartSet>();
                for (int i = 0; i < partsetCount; i++)
                {
                    int thisPartsetOffset = BitConverter.ToInt32(rawBytes, partsetOffset);
                    if(thisPartsetOffset != 0)
                    {
                        bcsFile.PartSets.Add(new PartSet() { Index = i.ToString() });
                        if (BitConverter.ToInt32(rawBytes, thisPartsetOffset + 20) != 10)
                        {
                            Console.WriteLine(String.Format("Part count mismatch on PartSet {0} (Expected 10, but found {1})\nThis BCS file cannot be parsed.", i, BitConverter.ToInt32(rawBytes, thisPartsetOffset + 20)));
                            Utils.WaitForInputThenQuit();
                        }

                        int tableOffset = thisPartsetOffset + BitConverter.ToInt32(rawBytes, thisPartsetOffset + 24);
                        
                        bcsFile.PartSets[actualIndex].FaceBase = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 0), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceForehead = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 4), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceEye = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 8), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceNose = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 12), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].FaceEar = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 16), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Hair = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 20), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Bust = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 24), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Pants = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 28), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Rist = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 32), thisPartsetOffset);
                        bcsFile.PartSets[actualIndex].Boots = ParsePart(BitConverter.ToInt32(rawBytes, tableOffset + 36), thisPartsetOffset);

                        actualIndex++;
                    }
                    partsetOffset += 4;
                }
            }

            //PartColors
            if(partcolorsCount > 0)
            {
                bcsFile.Part_Colors = new List<PartColor>();

                for(int i = 0; i < partcolorsCount; i++)
                {
                    int thisPartColorOffset = BitConverter.ToInt32(rawBytes, partcolorsOffset);

                    if(thisPartColorOffset != 0)
                    {
                        bcsFile.Part_Colors.Add(new PartColor()
                        {
                            Index = i.ToString(),
                            Str_00 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, thisPartColorOffset + 0) + thisPartColorOffset),
                            _Colors = ParseColors(BitConverter.ToInt32(rawBytes, thisPartColorOffset + 12) + thisPartColorOffset, BitConverter.ToInt16(rawBytes, thisPartColorOffset + 10))
                        });
                    }

                    partcolorsOffset += 4;
                }
            }


            //BodyScales
            if(bodyCount > 0)
            {
                bcsFile.Bodies = new List<Body>();

                for(int i = 0; i < bodyCount; i++)
                {
                    int thisBodyScaleOffset = BitConverter.ToInt32(rawBytes, bodyOffset);
                    if(thisBodyScaleOffset != 0)
                    {
                        bcsFile.Bodies.Add(ParseBody(thisBodyScaleOffset, i));
                    }
                    bodyOffset += 4;
                }

            }

            if(skeleton1Offset != 0)
            {
                bcsFile.SkeletonData1 = ParseSkeleton(BitConverter.ToInt32(rawBytes, skeleton1Offset));
            }
            if(skeleton2Offset != 0)
            {
                bcsFile.SkeletonData2 = ParseSkeleton(BitConverter.ToInt32(rawBytes, skeleton2Offset));
            }
            
            

        }


        //Part Parsers
        private Part ParsePart (int offset, int partOffset)
        {
            if(offset != 0)
            {
                offset += partOffset;

                return new Part()
                {
                    I_00 = BitConverter.ToInt16(rawBytes, offset + 0),
                    I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                    I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                    I_16 = BitConverter.ToInt16(rawBytes, offset + 16),
                    I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                    I_28 = BitConverter.ToInt32(rawBytes, offset + 28),
                    I_32 = BitConverter.ToInt32(rawBytes, offset + 32),
                    F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                    F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                    I_44 = BitConverter.ToInt32(rawBytes, offset + 44),
                    I_48 = BitConverter.ToInt32(rawBytes, offset + 48),
                    Str_52 = Utils.GetString(bytes, offset + 52, 4),
                    Str_56 = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 56), offset),
                    Str_60 = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 60), offset),
                    Str_64 = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 64), offset),
                    Str_68 = GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 68), offset),
                    Color_Selectors = ParseColorSelector(BitConverter.ToInt32(rawBytes, offset + 20) + offset, BitConverter.ToInt16(rawBytes, offset + 18)),
                    Physics_Objects = ParsePhysicsObject(BitConverter.ToInt32(rawBytes, offset + 76) + offset, BitConverter.ToInt16(rawBytes, offset + 74)),
                    Unk_3 = ParseUnk3(BitConverter.ToInt32(rawBytes, offset + 84) + offset, BitConverter.ToInt16(rawBytes, offset + 82))
                };

            } else
            {
                return null;
            }
            
        }

        private List<ColorSelector> ParseColorSelector(int offset, int count)
        {
            if(count > 0)
            {
                List<ColorSelector> colorSelectors = new List<ColorSelector>();

                for (int i = 0; i < count; i++)
                {
                    colorSelectors.Add(new ColorSelector()
                    {
                        I_00 = BitConverter.ToInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToInt16(rawBytes, offset + 2)
                    });
                    offset += 4;
                }
                return colorSelectors;
            }
            else
            {
                return null;
            }
            
        }

        private List<PhysicsObject> ParsePhysicsObject(int offset, int count)
        {
            if (count > 0)
            {
                List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

                for (int i = 0; i < count; i++)
                {
                    physicsObjects.Add(new PhysicsObject()
                    {
                        I_00 = BitConverter.ToInt16(rawBytes, offset + 0),
                        I_02 = BitConverter.ToInt16(rawBytes, offset + 2),
                        I_04 = BitConverter.ToInt16(rawBytes, offset + 4),
                        I_24 = BitConverter.ToInt32(rawBytes, offset + 24),
                        I_28 = BitConverter.ToInt16(rawBytes, offset + 28),
                        I_32 = BitConverter.ToInt16(rawBytes, offset + 32),
                        Str_36 = Utils.GetString(bytes, offset + 36, 4),
                        Str_40 = new string[6]
                        {
                            GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 40), offset),
                            GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 44), offset),
                            GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 48), offset),
                            GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 52), offset),
                            GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 56), offset),
                            GetStringWrapper(BitConverter.ToInt32(rawBytes, offset + 60), offset),
                        }
                    });
                    offset += 72;
                }
                return physicsObjects;
            }
            else
            {
                return null;
            }

        }
        
        private List<Unk3> ParseUnk3(int offset, int count)
        {
            if(count > 0)
            {
                List<Unk3> unk3 = new List<Unk3>();

                for(int i = 0; i < count; i++)
                {
                    unk3.Add(new Unk3()
                    {
                        I_00 = BitConverter_Ex.ToInt16Array(rawBytes, offset, 6)
                    });
                    offset += 12;
                }

                return unk3;

            }
            else
            {
                return null;
            }
        }

        //Color Parsers
        private List<Colors> ParseColors (int offset, int count)
        {
            if(count > 0)
            {
                List<Colors> colors = new List<Colors>();

                for(int i = 0; i < count; i++)
                {
                    colors.Add(new Colors()
                    {
                        Index = i.ToString(),
                        F_00 = BitConverter.ToSingle(rawBytes, offset + 0),
                        F_04 = BitConverter.ToSingle(rawBytes, offset + 4),
                        F_08 = BitConverter.ToSingle(rawBytes, offset + 8),
                        F_12 = BitConverter.ToSingle(rawBytes, offset + 12),
                        F_16 = BitConverter.ToSingle(rawBytes, offset + 16),
                        F_20 = BitConverter.ToSingle(rawBytes, offset + 20),
                        F_24 = BitConverter.ToSingle(rawBytes, offset + 24),
                        F_28 = BitConverter.ToSingle(rawBytes, offset + 28),
                        F_32 = BitConverter.ToSingle(rawBytes, offset + 32),
                        F_36 = BitConverter.ToSingle(rawBytes, offset + 36),
                        F_40 = BitConverter.ToSingle(rawBytes, offset + 40),
                        F_44 = BitConverter.ToSingle(rawBytes, offset + 44),
                        F_48 = BitConverter.ToSingle(rawBytes, offset + 48),
                        F_52 = BitConverter.ToSingle(rawBytes, offset + 52),
                        F_56 = BitConverter.ToSingle(rawBytes, offset + 56),
                        F_60 = BitConverter.ToSingle(rawBytes, offset + 60),
                        F_64 = BitConverter.ToSingle(rawBytes, offset + 64),
                        F_68 = BitConverter.ToSingle(rawBytes, offset + 68),
                        F_72 = BitConverter.ToSingle(rawBytes, offset + 72),
                        F_76 = BitConverter.ToSingle(rawBytes, offset + 76)
                    });

                    offset += 80;
                }

                return colors;
            }
            else
            {
                return null;
            }
        }
        
        //Body Parsers
        private Body ParseBody(int offset, int _index)
        {
            Body body = new Body() { BodyScales = new List<BoneScale>(), Index = _index.ToString() };
            int bodyCount = BitConverter.ToInt16(rawBytes, offset + 2);
            int bodyOffset = BitConverter.ToInt32(rawBytes, offset + 4) + offset;
            
            for(int i = 0; i < bodyCount; i++)
            {
                body.BodyScales.Add(new BoneScale()
                {
                    F_00 = BitConverter.ToSingle(rawBytes, bodyOffset + 0),
                    F_04 = BitConverter.ToSingle(rawBytes, bodyOffset + 4),
                    F_08 = BitConverter.ToSingle(rawBytes, bodyOffset + 8),
                    Str_12 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, bodyOffset + 12) + bodyOffset)
                });
                bodyOffset += 16;
            }

            return body;
        }

        //Skeleton Parsers
        private SkeletonData ParseSkeleton (int offset)
        {
            if(offset != 0)
            {
                SkeletonData skeleton = new SkeletonData();
                skeleton.I_00 = BitConverter.ToInt16(rawBytes, offset);
                int boneCount = BitConverter.ToInt16(rawBytes, offset + 2);
                int boneOffset = BitConverter.ToInt32(rawBytes, offset + 4) + offset;

                if (boneCount > 0)
                {
                    skeleton.Bones = new List<Bone>();

                    for (int i = 0; i < boneCount; i++)
                    {
                        skeleton.Bones.Add(new Bone()
                        {
                            I_00 = BitConverter.ToInt32(rawBytes, boneOffset + 0),
                            I_04 = BitConverter.ToInt32(rawBytes, boneOffset + 4),
                            F_12 = BitConverter.ToSingle(rawBytes, boneOffset + 12),
                            F_16 = BitConverter.ToSingle(rawBytes, boneOffset + 16),
                            F_20 = BitConverter.ToSingle(rawBytes, boneOffset + 20),
                            F_24 = BitConverter.ToSingle(rawBytes, boneOffset + 24),
                            F_28 = BitConverter.ToSingle(rawBytes, boneOffset + 28),
                            F_32 = BitConverter.ToSingle(rawBytes, boneOffset + 32),
                            F_36 = BitConverter.ToSingle(rawBytes, boneOffset + 36),
                            F_40 = BitConverter.ToSingle(rawBytes, boneOffset + 40),
                            F_44 = BitConverter.ToSingle(rawBytes, boneOffset + 44),
                            Str_48 = Utils.GetString(bytes, BitConverter.ToInt32(rawBytes, boneOffset + 48) + boneOffset)
                        });

                        boneOffset += 52;
                    }
                }

                return skeleton;
            }
            else
            {
                return null;
            }
        }

        //Utility
        
        private string GetStringWrapper(int relativeOffset, int mainOffset)
        {
            if(relativeOffset != 0)
            {
                return Utils.GetString(bytes, relativeOffset + mainOffset);
            } else
            {
                return "NULL";
            }
        }

    }
}
