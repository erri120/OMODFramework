using System.Collections.Generic;
using OblivionModManager.Scripting;

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

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

            var dlcbsa = sf.DataFileExists("DLCHorseArmor.bsa");
            var slof = sf.DataFileExists("Slof's Horses Base.esp");
            var slofessential = sf.DataFileExists("Slof's Horses Essential.esp");
            var slofv14 = sf.DataFileExists(@"textures\as\ashorsebay1.dds");
            var slofv20 = sf.DataFileExists(@"textures\creatures\horse\ashorse_bay1.dds");
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
                    var slofver = sf.Select(new[] {"Version 1.4", "Version 2.0"}, null,
                        new[]
                        {
                            "The latest version of Slof's Horses may be found at www.slofshive.co.uk",
                            "The latest version of Slof's Horses may be found at www.slofshive.co.uk"
                        }, "Which version of Slof's Horses are you running?", false);
                    switch (slofver[0])
                    {
                        case "Version 1.4":
                            sf.CopyPlugin("HRMHorseArmorSlofsHorsesPatch14.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                            sf.LoadBefore("HRMHorseArmor.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                            break;
                        case "Version 2.0":
                            sf.CopyPlugin("HRMHorseArmorSlofsHorsesPatch20.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                            sf.LoadBefore("HRMHorseArmor.esp", "HRMHorseArmorSlofsHorsesPatch.esp");
                            break;
                    }
                }
            }

            var clothType = sf.Select(new[] {"Knight", "King", "Green-Black", "Black-Gray"},
                new[]
                {
                    @"Textures\creatures\horse\Knight\Knight.jpg", @"Textures\creatures\horse\King\King.jpg",
                    @"Textures\creatures\horse\Green-Black\Green-Black.jpg",
                    @"Textures\creatures\horse\Black-Gray\Black-Gray.jpg"
                },
                new[]
                {
                    "For those of the highest honor", "For those of noble birth", "For the traveller",
                    "For the wanderer"
                }, "Choose a Cloth Horse Cover Type", false);
            switch (clothType[0])
            {
                case "Knight":
                {
                    var armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\Knight\armorcloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                    var armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\Knight\armorcloth_n.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                    var armorclothtex3 = sf.ReadDataFile(@"textures\creatures\horse\Knight\HRMHorseArmor_cloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds",
                        armorclothtex3);
                    break;
                }
                case "King":
                {
                    var armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\King\armorcloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                    var armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\King\armorcloth_n.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                    var armorclothtex3 = sf.ReadDataFile(@"textures\creatures\horse\King\HRMHorseArmor_cloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds",
                        armorclothtex3);
                    break;
                }
                case "Green-Black":
                {
                    var armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\Green-Black\armorcloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                    var armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\Green-Black\armorcloth_n.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                    var armorclothtex3 =
                        sf.ReadDataFile(@"textures\creatures\horse\Green-Black\HRMHorseArmor_cloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds",
                        armorclothtex3);
                    break;
                }
                case "Black-Gray":
                {
                    var armorclothtex1 = sf.ReadDataFile(@"textures\creatures\horse\Black-Gray\armorcloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth.dds", armorclothtex1);
                    var armorclothtex2 = sf.ReadDataFile(@"textures\creatures\horse\Black-Gray\armorcloth_n.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorcloth_n.dds", armorclothtex2);
                    var armorclothtex3 =
                        sf.ReadDataFile(@"textures\creatures\horse\Black-Gray\HRMHorseArmor_cloth.dds");
                    sf.GenerateNewDataFile(@"harlanrm\textures\menus\icons\armor\HRMHorseArmor_cloth.dds",
                        armorclothtex3);
                    break;
                }
            }

            var armorelventex1 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa", @"textures\creatures\horse\armorelven_n.dds");
            sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorelven_n.dds", armorelventex1);

            var armorsteeltex = sf.GetDataFileFromBSA("DLCHorseArmor.bsa", @"textures\creatures\horse\armorsteel.dds");
            sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorsteel.dds", armorsteeltex);

            var armorsteeltex1 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa", @"textures\creatures\horse\armorsteel_n.dds");
            sf.GenerateNewDataFile(@"harlanrm\textures\creatures\horse\armorsteel_n.dds", armorsteeltex1);

            var bridleelven = sf.GetDataFileFromBSA("DLCHorseArmor.bsa", @"meshes\creatures\horse\bridleelven.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridleelven.nif", bridleelven);

            var bridleglass1 = new List<byte>(bridleelven);
            bridleglass1.RemoveRange(0xBE0B, 5);
            bridleglass1.InsertRange(0xBE0B, new byte[] {0x47, 0x6C, 0x61, 0x73, 0x73});
            var bridleglass = bridleglass1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridleglass.nif", bridleglass);

            var bridlechainmail1 = new List<byte>(bridleelven) {[0xBDE9] = 0x2B};
            bridlechainmail1.RemoveRange(0xBE0B, 5);
            bridlechainmail1.InsertRange(0xBE0B, new byte[] {0x43, 0x68, 0x61, 0x69, 0x6E, 0x6D, 0x61, 0x69, 0x6C});
            var bridlechainmail = bridlechainmail1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlechainmail.nif", bridlechainmail);

            var bridlecloth1 = new List<byte>(bridleelven);
            bridlecloth1.RemoveRange(0xBE0B, 5);
            bridlecloth1.InsertRange(0xBE0B, new byte[] {0x43, 0x6C, 0x6F, 0x74, 0x68});
            var bridlecloth = bridlecloth1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlecloth.nif", bridlecloth);

            var bridlesteel = sf.GetDataFileFromBSA("DLCHorseArmor.bsa", @"meshes\creatures\horse\bridlesteel.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlesteel.nif", bridlesteel);

            var
                bridlelegion1 = new List<byte>(bridlesteel) {[0xD141] = 0x28};
            bridlelegion1.RemoveRange(0xD163, 5);
            bridlelegion1.InsertRange(0xD163, new byte[] {0x4C, 0x65, 0x67, 0x69, 0x6F, 0x6E});
            var bridlelegion = bridlelegion1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridlelegion.nif", bridlelegion);

            var
                bridledragon1 = new List<byte>(bridlesteel) {[0xD141] = 0x28};
            bridledragon1.RemoveRange(0xD163, 5);
            bridledragon1.InsertRange(0xD163, new byte[] {0x44, 0x72, 0x61, 0x67, 0x6F, 0x6E});
            var bridledragon = bridledragon1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridledragon.nif", bridledragon);

            var bridleebony1 = new List<byte>(bridlesteel);
            bridleebony1.RemoveRange(0xD163, 5);
            bridleebony1.InsertRange(0xD163, new byte[] {0x45, 0x62, 0x6F, 0x6E, 0x79});
            var bridleebony = bridleebony1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridleebony.nif", bridleebony);

            var bridledaedric1 = new List<byte>(bridlesteel) {[0xD141] = 0x29};
            bridledaedric1.RemoveRange(0xD163, 5);
            bridledaedric1.InsertRange(0xD163, new byte[] {0x44, 0x61, 0x65, 0x64, 0x72, 0x69, 0x63});
            var bridledaedric = bridledaedric1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\bridledaedric.nif", bridledaedric);

            var armorelven = sf.GetDataFileFromBSA("DLCHorseArmor.bsa", @"meshes\creatures\horse\armorelven.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorelven.nif", armorelven);

            var armorcloth1 = new List<byte>(armorelven);
            armorcloth1.RemoveRange(0x9C86, 5);
            armorcloth1.InsertRange(0x9C86, new byte[] {0x43, 0x6C, 0x6F, 0x74, 0x68});
            var armorcloth = armorcloth1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorcloth.nif", armorcloth);

            var armorchainmail1 = new List<byte>(armorelven) {[0x9C64] = 0x2B};
            armorchainmail1.RemoveRange(0x9C86, 5);
            armorchainmail1.InsertRange(0x9C86, new byte[] {0x43, 0x68, 0x61, 0x69, 0x6E, 0x6D, 0x61, 0x69, 0x6C});
            var armorchainmail = armorchainmail1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorchainmail.nif", armorchainmail);

            var armorglass1 = new List<byte>(armorelven);
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
            var armorglass = armorglass1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorglass.nif", armorglass);

            var armorsteel = sf.GetDataFileFromBSA("DLCHorseArmor.bsa", @"meshes\creatures\horse\armorsteel.nif");
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorsteel.nif", armorsteel);

            var armorebony1 = new List<byte>(armorsteel);
            armorebony1.RemoveRange(0x11CFD, 5);
            armorebony1.InsertRange(0x11CFD, new byte[] {0x45, 0x62, 0x6F, 0x6E, 0x79});
            var armorebony = armorebony1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorebony.nif", armorebony);

            var armordaedric1 = new List<byte>(armorsteel) {[0x11CDB] = 0x29};
            armordaedric1.RemoveRange(0x11CFD, 5);
            armordaedric1.InsertRange(0x11CFD, new byte[] {0x44, 0x61, 0x65, 0x64, 0x72, 0x69, 0x63});
            var armordaedric = armordaedric1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armordaedric.nif", armordaedric);

            var armorlegion1 = new List<byte>(armorsteel) {[0x11CDB] = 0x28};
            armorlegion1.RemoveRange(0x11CFD, 5);
            armorlegion1.InsertRange(0x11CFD, new byte[] {0x4C, 0x65, 0x67, 0x69, 0x6F, 0x6E});
            var armorlegion = armorlegion1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armorlegion.nif", armorlegion);

            var armordragon1 = new List<byte>(armorsteel) {[0x11CDB] = 0x28};
            armordragon1.RemoveRange(0x11CFD, 5);
            armordragon1.InsertRange(0x11CFD, new byte[] {0x44, 0x72, 0x61, 0x67, 0x6F, 0x6E});
            var armordragon = armordragon1.ToArray();
            sf.GenerateNewDataFile(@"harlanrm\meshes\creatures\horse\armordragon.nif", armordragon);

            var foot1 = sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
                @"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_01.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_01.wav", foot1);

            var foot2 = sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
                @"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_02.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_02.wav", foot2);

            var foot3 = sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
                @"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_03.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_03.wav", foot3);

            var foot4 = sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
                @"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_04.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_04.wav", foot4);

            var foot5 = sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
                @"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_05.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_05.wav", foot5);

            var foot6 = sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
                @"sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_06.wav");
            sf.GenerateNewDataFile(@"harlanrm\sound\fx\npc\horse\foot\armor\npc_horse_foot_armor_06.wav", foot6);

            var topic0 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
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

            var topic1 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
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

            var topicbuy0 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
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

            var topicbuy1 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
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

            var topicelven0 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
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

            var topicelven1 =
                sf.GetDataFileFromBSA("DLCHorseArmor.bsa",
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

            var topiccancel0 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091e5_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicCancel_0000D57C_1.lip",
                topiccancel0);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonCancel_0000B3A3_1.lip",
                topiccancel0);
            var topiccancel1 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091e5_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicCancel_0000D57C_1.mp3",
                topiccancel1);
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonCancel_0000B3A3_1.mp3",
                topiccancel1);

            var topicrefund0 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_goodbye_0002b7ac_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicRefund_0000C150_1.lip",
                topicrefund0);
            var topicrefund1 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_goodbye_0002b7ac_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmor_HRMHorseArmorTopicRefund_0000C150_1.mp3",
                topicrefund1);

            var topicserve0 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091ea_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B39F_1.lip",
                topicserve0);
            var topicserve1 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_barterexit_000091ea_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B39F_1.mp3",
                topicserve1);

            var topicdragon0 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\emfriddemo_greeting_00028a26_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B3A0_1.lip",
                topicdragon0);
            var topicdragon1 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\emfriddemo_greeting_00028a26_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragon_0000B3A0_1.mp3",
                topicdragon1);

            var topicdragondecline0 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_barterfail_0000921e_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonBuy_0000B3A5_1.lip",
                topicdragondecline0);
            var topicdragondecline1 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_barterfail_0000921e_1.mp3");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonBuy_0000B3A5_1.mp3",
                topicdragondecline1);

            var topicdragonbuy0 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_persuasionenter_0018b2e5_1.lip");
            sf.GenerateNewDataFile(
                @"harlanrm\sound\Voice\HRMHorseArmor.esp\nord\f\HRMHorseArmorDragon_HRMHorseArmorTopicDragonBuy_0000B3A6_1.lip",
                topicdragonbuy0);
            var topicdragonbuy1 =
                sf.GetDataFileFromBSA("Oblivion - Voices2.bsa",
                    @"sound\voice\oblivion.esm\nord\f\generic_persuasionenter_0018b2e5_1.mp3");
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
