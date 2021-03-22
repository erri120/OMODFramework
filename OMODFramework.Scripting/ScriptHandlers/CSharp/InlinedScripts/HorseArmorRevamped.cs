using OblivionModManager.Scripting;

//TODO: cleanup
//ReSharper disable all
#pragma warning disable 414
#nullable disable

/*
 * Original mod: https://www.nexusmods.com/oblivion/mods/46657
 */

#region Horse Armor Revamped 1.8

namespace OMODFramework.Scripting.ScriptHandlers.CSharp.InlinedScripts
{
    internal class HorseArmorRevamped : IScript
    {
        internal const uint CRC = 0x9646E015;
        
        void IScript.Execute(IScriptFunctions sf)
        {
            if (sf.GetOBMMVersion() < new System.Version("1.1.9"))
            {
                System.Windows.Forms.MessageBox.Show("This omod requires obmm 1.1.9 or later", "Error");
                sf.FatalError();
                return;
            }

            bool dlcbsa = sf.DataFileExists("DLCHorseArmor.bsa");
            bool slof = sf.DataFileExists("Slof's Horses Base.esp");
            bool slofessential = sf.DataFileExists("Slof's Horses Essential.esp");
            bool slofv14 = sf.DataFileExists(@"textures\as\ashorsebay1.dds");
            bool slofv20 = sf.DataFileExists(@"textures\creatures\horse\ashorse_bay1.dds");
            if (!dlcbsa)
            {
                System.Windows.Forms.MessageBox.Show("I could not find DLCHorseArmor.bsa, aborting installation.",
                    "Error");
                System.Windows.Forms.MessageBox.Show("This omod requires the official Bethesda Horse Armor Plugin.",
                    "Error");
                sf.FatalError();
                return;
            }

            if (slofessential)
            {
                System.Windows.Forms.MessageBox.Show(
                    "I found 'Slof's Horses Essential.esp'.  Please upgrade Slof's Horse to version 2.0 at www.slofshive.co.uk",
                    "Error");
            }

            if (slof)
            {
                if (slofv20)
                {
                    sf.CopyPlugin("HRMHorseArmorSlofsHorsesPatch20.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                    sf.LoadBefore("HRMHorseArmor.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                }
                else if (slofv14)
                {
                    sf.CopyPlugin("HRMHorseArmorSlofsHorsesPatch14.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                    sf.LoadBefore("HRMHorseArmor.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                }
                else
                {
                    string[] slofver = sf.Select(new string[] {"Version 1.4", "Version 2.0"}, new string[] {null, null},
                        new string[]
                        {
                            "The latest version of Slof's Horses may be found at www.slofshive.co.uk",
                            "The latest version of Slof's Horses may be found at www.slofshive.co.uk"
                        }, "Which version of Slof's Horses are you running?", false);
                    if (slofver[0] == "Version 1.4")
                    {
                        sf.CopyPlugin("HRMHorseArmorSlofsHorsesPatch14.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                        sf.LoadBefore("HRMHorseArmor.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                    }
                    else if (slofver[0] == "Version 2.0")
                    {
                        sf.CopyPlugin("HRMHorseArmorSlofsHorsesPatch20.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                        sf.LoadBefore("HRMHorseArmor.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                    }
                }
            }

            string[] clothType = sf.Select(new string[] {"Knight", "King", "Green-Black", "Black-Gray"},
                new string[]
                {
                    @"Textures\creatures\horse\Knight\Knight.jpg", @"Textures\creatures\horse\King\King.jpg",
                    @"Textures\creatures\horse\Green-Black\Green-Black.jpg",
                    @"Textures\creatures\horse\Black-Gray\Black-Gray.jpg"
                },
                new string[]
                {
                    "For those of the highest honor", "For those of noble birth", "For the traveller",
                    "For the wanderer"
                }, "Choose a Cloth Horse Cover Type", false);
            if (clothType[0] == "Knight")
            {
                byte[] armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\Knight\armorcloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                byte[] armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\Knight\armorcloth_n.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                byte[] armorclothtex3 = sf.ReadDataFile(@"textures\creatures\horse\Knight\HRMHorseArmor_cloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds", armorclothtex3);
            }
            else if (clothType[0] == "King")
            {
                byte[] armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\King\armorcloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                byte[] armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\King\armorcloth_n.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                byte[] armorclothtex3 = sf.ReadDataFile(@"textures\creatures\horse\King\HRMHorseArmor_cloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds", armorclothtex3);
            }
            else if (clothType[0] == "Green-Black")
            {
                byte[] armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\Green-Black\armorcloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                byte[] armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\Green-Black\armorcloth_n.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                byte[] armorclothtex3 =
                    sf.ReadDataFile(@"textures\creatures\horse\Green-Black\HRMHorseArmor_cloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds", armorclothtex3);
            }
            else if (clothType[0] == "Black-Gray")
            {
                byte[] armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\Black-Gray\armorcloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                byte[] armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\Black-Gray\armorcloth_n.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                byte[] armorclothtex3 = sf.ReadDataFile(@"textures\creatures\horse\Black-Gray\HRMHorseArmor_cloth.dds");
                sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds", armorclothtex3);
            }

            byte[] armorelventex1 = sf.GetDataFileFromBSA(@"textures\creatures\horse\armorelven_n.dds");
            sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorelven_n.dds", armorelventex1);

            byte[] armorsteeltex = sf.GetDataFileFromBSA(@"textures\creatures\horse\armorsteel.dds");
            sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorsteel.dds", armorsteeltex);

            byte[] armorsteeltex1 = sf.GetDataFileFromBSA(@"textures\creatures\horse\armorsteel_n.dds");
            sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorsteel_n.dds", armorsteeltex1);

            byte[] bridleelven = sf.GetDataFileFromBSA(@"meshes\creatures\horse\bridleelven.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridleelven.nif", bridleelven);

            System.Collections.Generic.List<byte> bridleglass1 = new System.Collections.Generic.List<byte>(bridleelven);
            bridleglass1.RemoveRange(0xBE0B, 5);
            bridleglass1.InsertRange(0xBE0B, new byte[] {0x47, 0x6C, 0x61, 0x73, 0x73});
            byte[] bridleglass = bridleglass1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridleglass.nif", bridleglass);

            System.Collections.Generic.List<byte> bridlechainmail1 =
                new System.Collections.Generic.List<byte>(bridleelven);
            bridlechainmail1[0xBDE9] = 0x2B;
            bridlechainmail1.RemoveRange(0xBE0B, 5);
            bridlechainmail1.InsertRange(0xBE0B, new byte[] {0x43, 0x68, 0x61, 0x69, 0x6E, 0x6D, 0x61, 0x69, 0x6C});
            byte[] bridlechainmail = bridlechainmail1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlechainmail.nif", bridlechainmail);

            System.Collections.Generic.List<byte> bridlecloth1 = new System.Collections.Generic.List<byte>(bridleelven);
            bridlecloth1.RemoveRange(0xBE0B, 5);
            bridlecloth1.InsertRange(0xBE0B, new byte[] {0x43, 0x6C, 0x6F, 0x74, 0x68});
            byte[] bridlecloth = bridlecloth1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlecloth.nif", bridlecloth);

            byte[] bridlesteel = sf.GetDataFileFromBSA(@"meshes\creatures\horse\bridlesteel.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlesteel.nif", bridlesteel);

            System.Collections.Generic.List<byte>
                bridlelegion1 = new System.Collections.Generic.List<byte>(bridlesteel);
            bridlelegion1[0xD141] = 0x28;
            bridlelegion1.RemoveRange(0xD163, 5);
            bridlelegion1.InsertRange(0xD163, new byte[] {0x4C, 0x65, 0x67, 0x69, 0x6F, 0x6E});
            byte[] bridlelegion = bridlelegion1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlelegion.nif", bridlelegion);

            System.Collections.Generic.List<byte>
                bridledragon1 = new System.Collections.Generic.List<byte>(bridlesteel);
            bridledragon1[0xD141] = 0x28;
            bridledragon1.RemoveRange(0xD163, 5);
            bridledragon1.InsertRange(0xD163, new byte[] {0x44, 0x72, 0x61, 0x67, 0x6F, 0x6E});
            byte[] bridledragon = bridledragon1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridledragon.nif", bridledragon);

            System.Collections.Generic.List<byte> bridleebony1 = new System.Collections.Generic.List<byte>(bridlesteel);
            bridleebony1.RemoveRange(0xD163, 5);
            bridleebony1.InsertRange(0xD163, new byte[] {0x45, 0x62, 0x6F, 0x6E, 0x79});
            byte[] bridleebony = bridleebony1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridleebony.nif", bridleebony);

            System.Collections.Generic.List<byte> bridledaedric1 =
                new System.Collections.Generic.List<byte>(bridlesteel);
            bridledaedric1[0xD141] = 0x29;
            bridledaedric1.RemoveRange(0xD163, 5);
            bridledaedric1.InsertRange(0xD163, new byte[] {0x44, 0x61, 0x65, 0x64, 0x72, 0x69, 0x63});
            byte[] bridledaedric = bridledaedric1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridledaedric.nif", bridledaedric);

            byte[] armorelven = sf.GetDataFileFromBSA(@"meshes\creatures\horse\armorelven.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorelven.nif", armorelven);

            System.Collections.Generic.List<byte> armorcloth1 = new System.Collections.Generic.List<byte>(armorelven);
            armorcloth1.RemoveRange(0x9C86, 5);
            armorcloth1.InsertRange(0x9C86, new byte[] {0x43, 0x6C, 0x6F, 0x74, 0x68});
            byte[] armorcloth = armorcloth1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorcloth.nif", armorcloth);

            System.Collections.Generic.List<byte> armorchainmail1 =
                new System.Collections.Generic.List<byte>(armorelven);
            armorchainmail1[0x9C64] = 0x2B;
            armorchainmail1.RemoveRange(0x9C86, 5);
            armorchainmail1.InsertRange(0x9C86, new byte[] {0x43, 0x68, 0x61, 0x69, 0x6E, 0x6D, 0x61, 0x69, 0x6C});
            byte[] armorchainmail = armorchainmail1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorchainmail.nif", armorchainmail);

            System.Collections.Generic.List<byte> armorglass1 = new System.Collections.Generic.List<byte>(armorelven);
            armorglass1.RemoveRange(0x9BCC, 10);
            armorglass1.InsertRange(0x9BCC, new byte[] {0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F});
            armorglass1.RemoveRange(0x9BE2, 12);
            armorglass1.InsertRange(0x9BE2,
                new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00});
            armorglass1.RemoveRange(0x9BF0, 10);
            armorglass1.InsertRange(0x9BF0, new byte[] {0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F});
            armorglass1[0x9C33] = 0x03;
            armorglass1.RemoveRange(0x9C86, 5);
            armorglass1.InsertRange(0x9C86, new byte[] {0x47, 0x6C, 0x61, 0x73, 0x73});
            armorglass1[0x9BB3] = 0x07;
            armorglass1.RemoveRange(0x9BB7, 11);
            armorglass1.InsertRange(0x9BB7, new byte[] {0x45, 0x6E, 0x76, 0x4D, 0x61, 0x70, 0x32});
            byte[] armorglass = armorglass1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorglass.nif", armorglass);

            byte[] armorsteel = sf.GetDataFileFromBSA(@"meshes\creatures\horse\armorsteel.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorsteel.nif", armorsteel);

            System.Collections.Generic.List<byte> armorebony1 = new System.Collections.Generic.List<byte>(armorsteel);
            armorebony1.RemoveRange(0x11CFD, 5);
            armorebony1.InsertRange(0x11CFD, new byte[] {0x45, 0x62, 0x6F, 0x6E, 0x79});
            byte[] armorebony = armorebony1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorebony.nif", armorebony);

            System.Collections.Generic.List<byte> armordaedric1 = new System.Collections.Generic.List<byte>(armorsteel);
            armordaedric1[0x11CDB] = 0x29;
            armordaedric1.RemoveRange(0x11CFD, 5);
            armordaedric1.InsertRange(0x11CFD, new byte[] {0x44, 0x61, 0x65, 0x64, 0x72, 0x69, 0x63});
            byte[] armordaedric = armordaedric1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armordaedric.nif", armordaedric);

            System.Collections.Generic.List<byte> armorlegion1 = new System.Collections.Generic.List<byte>(armorsteel);
            armorlegion1[0x11CDB] = 0x28;
            armorlegion1.RemoveRange(0x11CFD, 5);
            armorlegion1.InsertRange(0x11CFD, new byte[] {0x4C, 0x65, 0x67, 0x69, 0x6F, 0x6E});
            byte[] armorlegion = armorlegion1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorlegion.nif", armorlegion);

            System.Collections.Generic.List<byte> armordragon1 = new System.Collections.Generic.List<byte>(armorsteel);
            armordragon1[0x11CDB] = 0x28;
            armordragon1.RemoveRange(0x11CFD, 5);
            armordragon1.InsertRange(0x11CFD, new byte[] {0x44, 0x72, 0x61, 0x67, 0x6F, 0x6E});
            byte[] armordragon = armordragon1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armordragon.nif", armordragon);

            byte[] foot1 = sf.GetDataFileFromBSA(@"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_01.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_01.wav", foot1);

            byte[] foot2 = sf.GetDataFileFromBSA(@"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_02.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_02.wav", foot2);

            byte[] foot3 = sf.GetDataFileFromBSA(@"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_03.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_03.wav", foot3);

            byte[] foot4 = sf.GetDataFileFromBSA(@"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_04.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_04.wav", foot4);

            byte[] foot5 = sf.GetDataFileFromBSA(@"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_05.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_05.wav", foot5);

            byte[] foot6 = sf.GetDataFileFromBSA(@"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_06.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_06.wav", foot6);

            byte[] topic0 =
                sf.GetDataFileFromBSA(
                    @"sound\voice\dlchorsearmor.esp\nord\f\dlchorsearmor_dlchorsearmortopic_00000cf0_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00000cf0_1.lip",
                topic0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A40_1.lip",
                topic0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A41_1.lip",
                topic0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A42_1.lip",
                topic0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A43_1.lip",
                topic0);

            byte[] topic1 =
                sf.GetDataFileFromBSA(
                    @"sound\voice\dlchorsearmor.esp\nord\f\dlchorsearmor_dlchorsearmortopic_00000cf0_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00000cf0_1.mp3",
                topic1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A40_1.mp3",
                topic1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A41_1.mp3",
                topic1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A42_1.mp3",
                topic1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopic_00002A43_1.mp3",
                topic1);

            byte[] topicbuy0 =
                sf.GetDataFileFromBSA(
                    @"sound\voice\dlchorsearmor.esp\nord\f\dlchorsearmor_dlchorsearmortopicbuy_0000210e_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicDaedric_0000351E_1.lip",
                topicbuy0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicEbony_0000C7D1_1.lip",
                topicbuy0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicElven_0000C7D3_1.lip",
                topicbuy0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicGlass_0000C7D2_1.lip",
                topicbuy0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicSteel_0000C7D4_1.lip",
                topicbuy0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicChain_00006181_1.lip",
                topicbuy0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicCloth_0000A5E0_1.lip",
                topicbuy0);

            byte[] topicbuy1 =
                sf.GetDataFileFromBSA(
                    @"sound\voice\dlchorsearmor.esp\nord\f\dlchorsearmor_dlchorsearmortopicbuy_0000210e_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicDaedric_0000351E_1.mp3",
                topicbuy1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicEbony_0000C7D1_1.mp3",
                topicbuy1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicElven_0000C7D3_1.mp3",
                topicbuy1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicGlass_0000C7D2_1.mp3",
                topicbuy1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicSteel_0000C7D4_1.mp3",
                topicbuy1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicChain_00006181_1.mp3",
                topicbuy1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicCloth_0000A5E0_1.mp3",
                topicbuy1);

            byte[] topicelven0 =
                sf.GetDataFileFromBSA(
                    @"sound\voice\dlchorsearmor.esp\nord\f\dlchorsearmor_dlchorsearmortopicelven_00002613_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EDF_1.lip",
                topicelven0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EE0_1.lip",
                topicelven0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EE1_1.lip",
                topicelven0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EE2_1.lip",
                topicelven0);

            byte[] topicelven1 =
                sf.GetDataFileFromBSA(
                    @"sound\voice\dlchorsearmor.esp\nord\f\dlchorsearmor_dlchorsearmortopicelven_00002613_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EDF_1.mp3",
                topicelven1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EE0_1.mp3",
                topicelven1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EE1_1.mp3",
                topicelven1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicHelp_00000EE2_1.mp3",
                topicelven1);

            byte[] topiccancel0 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091e5_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicCancel_0000D57C_1.lip",
                topiccancel0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonCancel_0000B3A3_1.lip",
                topiccancel0);
            byte[] topiccancel1 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091e5_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicCancel_0000D57C_1.mp3",
                topiccancel1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonCancel_0000B3A3_1.mp3",
                topiccancel1);

            byte[] topicrefund0 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_goodbye_0002b7ac_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicRefund_0000C150_1.lip",
                topicrefund0);
            byte[] topicrefund1 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_goodbye_0002b7ac_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicRefund_0000C150_1.mp3",
                topicrefund1);

            byte[] topicserve0 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091ea_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B39F_1.lip",
                topicserve0);
            byte[] topicserve1 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091ea_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B39F_1.mp3",
                topicserve1);

            byte[] topicdragon0 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\emfriddemo_greeting_00028a26_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B3A0_1.lip",
                topicdragon0);
            byte[] topicdragon1 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\emfriddemo_greeting_00028a26_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B3A0_1.mp3",
                topicdragon1);

            byte[] topicdragondecline0 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_barterfail_0000921e_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonBuy_0000B3A5_1.lip",
                topicdragondecline0);
            byte[] topicdragondecline1 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_barterfail_0000921e_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonBuy_0000B3A5_1.mp3",
                topicdragondecline1);

            byte[] topicdragonbuy0 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_persuasionenter_0018b2e5_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonBuy_0000B3A6_1.lip",
                topicdragonbuy0);
            byte[] topicdragonbuy1 =
                sf.GetDataFileFromBSA(@"sound\voice\oblivion.esm\nord\f\generic_persuasionenter_0018b2e5_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonBuy_0000B3A6_1.mp3",
                topicdragonbuy1);

            sf.GenerateBSA("HRMHorseArmor.bsa", "harlanrm", "", 0, 0);
            sf.CancelDataFolderCopy("harlanrm");
            sf.DontInstallDataFolder("harlanrm", true);
            sf.DontInstallDataFolder(@"textures\creatures\horse\Knight", true);
            sf.DontInstallDataFolder(@"textures\creatures\horse\King", true);
            sf.DontInstallDataFolder(@"textures\creatures\horse\Green-Black", true);
            sf.DontInstallDataFolder(@"textures\creatures\horse\Black-Gray", true);
            sf.DontInstallPlugin("HRMHorseArmorSlofsHorsesPatch14.esp");
            sf.DontInstallPlugin("HRMHorseArmorSlofsHorsesPatch20.esp");
        }
    }
}

#endregion
