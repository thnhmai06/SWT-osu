// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Users;

namespace osu.Game.Tournament.Tests
{
    [TestFixture]
    public class CountryExtensionsTest
    {
        // ISO 3166-1 alpha-3 codes, one case per switch label.
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BD
        [TestCase(CountryCode.BD, "BGD")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BE
        [TestCase(CountryCode.BE, "BEL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BF
        [TestCase(CountryCode.BF, "BFA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BG
        [TestCase(CountryCode.BG, "BGR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BA
        [TestCase(CountryCode.BA, "BIH")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BB
        [TestCase(CountryCode.BB, "BRB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.WF
        [TestCase(CountryCode.WF, "WLF")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BL
        [TestCase(CountryCode.BL, "BLM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BM
        [TestCase(CountryCode.BM, "BMU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BN
        [TestCase(CountryCode.BN, "BRN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BO
        [TestCase(CountryCode.BO, "BOL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BH
        [TestCase(CountryCode.BH, "BHR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BI
        [TestCase(CountryCode.BI, "BDI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BJ
        [TestCase(CountryCode.BJ, "BEN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BT
        [TestCase(CountryCode.BT, "BTN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.JM
        [TestCase(CountryCode.JM, "JAM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BV
        [TestCase(CountryCode.BV, "BVT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BW
        [TestCase(CountryCode.BW, "BWA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.WS
        [TestCase(CountryCode.WS, "WSM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BQ
        [TestCase(CountryCode.BQ, "BES")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BR
        [TestCase(CountryCode.BR, "BRA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BS
        [TestCase(CountryCode.BS, "BHS")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.JE
        [TestCase(CountryCode.JE, "JEY")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BY
        [TestCase(CountryCode.BY, "BLR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.BZ
        [TestCase(CountryCode.BZ, "BLZ")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.RU
        [TestCase(CountryCode.RU, "RUS")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.RW
        [TestCase(CountryCode.RW, "RWA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.RS
        [TestCase(CountryCode.RS, "SRB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TL
        [TestCase(CountryCode.TL, "TLS")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.RE
        [TestCase(CountryCode.RE, "REU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TM
        [TestCase(CountryCode.TM, "TKM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TJ
        [TestCase(CountryCode.TJ, "TJK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.RO
        [TestCase(CountryCode.RO, "ROU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TK
        [TestCase(CountryCode.TK, "TKL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GW
        [TestCase(CountryCode.GW, "GNB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GU
        [TestCase(CountryCode.GU, "GUM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GT
        [TestCase(CountryCode.GT, "GTM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GS
        [TestCase(CountryCode.GS, "SGS")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GR
        [TestCase(CountryCode.GR, "GRC")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GQ
        [TestCase(CountryCode.GQ, "GNQ")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GP
        [TestCase(CountryCode.GP, "GLP")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.JP
        [TestCase(CountryCode.JP, "JPN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GY
        [TestCase(CountryCode.GY, "GUY")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GG
        [TestCase(CountryCode.GG, "GGY")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GF
        [TestCase(CountryCode.GF, "GUF")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GE
        [TestCase(CountryCode.GE, "GEO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GD
        [TestCase(CountryCode.GD, "GRD")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GB
        [TestCase(CountryCode.GB, "GBR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GA
        [TestCase(CountryCode.GA, "GAB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SV
        [TestCase(CountryCode.SV, "SLV")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GN
        [TestCase(CountryCode.GN, "GIN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GM
        [TestCase(CountryCode.GM, "GMB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GL
        [TestCase(CountryCode.GL, "GRL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GI
        [TestCase(CountryCode.GI, "GIB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.GH
        [TestCase(CountryCode.GH, "GHA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.OM
        [TestCase(CountryCode.OM, "OMN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TN
        [TestCase(CountryCode.TN, "TUN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.JO
        [TestCase(CountryCode.JO, "JOR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.HR
        [TestCase(CountryCode.HR, "HRV")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.HT
        [TestCase(CountryCode.HT, "HTI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.HU
        [TestCase(CountryCode.HU, "HUN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.HK
        [TestCase(CountryCode.HK, "HKG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.HN
        [TestCase(CountryCode.HN, "HND")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.HM
        [TestCase(CountryCode.HM, "HMD")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.VE
        [TestCase(CountryCode.VE, "VEN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PR
        [TestCase(CountryCode.PR, "PRI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PS
        [TestCase(CountryCode.PS, "PSE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PW
        [TestCase(CountryCode.PW, "PLW")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PT
        [TestCase(CountryCode.PT, "PRT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SJ
        [TestCase(CountryCode.SJ, "SJM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PY
        [TestCase(CountryCode.PY, "PRY")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IQ
        [TestCase(CountryCode.IQ, "IRQ")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PA
        [TestCase(CountryCode.PA, "PAN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PF
        [TestCase(CountryCode.PF, "PYF")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PG
        [TestCase(CountryCode.PG, "PNG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PE
        [TestCase(CountryCode.PE, "PER")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PK
        [TestCase(CountryCode.PK, "PAK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PH
        [TestCase(CountryCode.PH, "PHL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PN
        [TestCase(CountryCode.PN, "PCN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PL
        [TestCase(CountryCode.PL, "POL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.PM
        [TestCase(CountryCode.PM, "SPM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ZM
        [TestCase(CountryCode.ZM, "ZMB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.EH
        [TestCase(CountryCode.EH, "ESH")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.EE
        [TestCase(CountryCode.EE, "EST")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.EG
        [TestCase(CountryCode.EG, "EGY")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ZA
        [TestCase(CountryCode.ZA, "ZAF")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.EC
        [TestCase(CountryCode.EC, "ECU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IT
        [TestCase(CountryCode.IT, "ITA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.VN
        [TestCase(CountryCode.VN, "VNM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SB
        [TestCase(CountryCode.SB, "SLB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ET
        [TestCase(CountryCode.ET, "ETH")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SO
        [TestCase(CountryCode.SO, "SOM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ZW
        [TestCase(CountryCode.ZW, "ZWE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SA
        [TestCase(CountryCode.SA, "SAU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ES
        [TestCase(CountryCode.ES, "ESP")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ER
        [TestCase(CountryCode.ER, "ERI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ME
        [TestCase(CountryCode.ME, "MNE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MD
        [TestCase(CountryCode.MD, "MDA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MG
        [TestCase(CountryCode.MG, "MDG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MF
        [TestCase(CountryCode.MF, "MAF")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MA
        [TestCase(CountryCode.MA, "MAR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MC
        [TestCase(CountryCode.MC, "MCO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.UZ
        [TestCase(CountryCode.UZ, "UZB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MM
        [TestCase(CountryCode.MM, "MMR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ML
        [TestCase(CountryCode.ML, "MLI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MO
        [TestCase(CountryCode.MO, "MAC")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MN
        [TestCase(CountryCode.MN, "MNG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MH
        [TestCase(CountryCode.MH, "MHL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MK
        [TestCase(CountryCode.MK, "MKD")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MU
        [TestCase(CountryCode.MU, "MUS")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MT
        [TestCase(CountryCode.MT, "MLT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MW
        [TestCase(CountryCode.MW, "MWI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MV
        [TestCase(CountryCode.MV, "MDV")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MQ
        [TestCase(CountryCode.MQ, "MTQ")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MP
        [TestCase(CountryCode.MP, "MNP")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MS
        [TestCase(CountryCode.MS, "MSR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MR
        [TestCase(CountryCode.MR, "MRT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IM
        [TestCase(CountryCode.IM, "IMN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.UG
        [TestCase(CountryCode.UG, "UGA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TZ
        [TestCase(CountryCode.TZ, "TZA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MY
        [TestCase(CountryCode.MY, "MYS")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MX
        [TestCase(CountryCode.MX, "MEX")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IL
        [TestCase(CountryCode.IL, "ISR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.FR
        [TestCase(CountryCode.FR, "FRA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IO
        [TestCase(CountryCode.IO, "IOT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SH
        [TestCase(CountryCode.SH, "SHN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.FI
        [TestCase(CountryCode.FI, "FIN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.FJ
        [TestCase(CountryCode.FJ, "FJI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.FK
        [TestCase(CountryCode.FK, "FLK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.FM
        [TestCase(CountryCode.FM, "FSM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.FO
        [TestCase(CountryCode.FO, "FRO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NI
        [TestCase(CountryCode.NI, "NIC")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NL
        [TestCase(CountryCode.NL, "NLD")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NO
        [TestCase(CountryCode.NO, "NOR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NA
        [TestCase(CountryCode.NA, "NAM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.VU
        [TestCase(CountryCode.VU, "VUT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NC
        [TestCase(CountryCode.NC, "NCL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NE
        [TestCase(CountryCode.NE, "NER")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NF
        [TestCase(CountryCode.NF, "NFK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NG
        [TestCase(CountryCode.NG, "NGA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NZ
        [TestCase(CountryCode.NZ, "NZL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NP
        [TestCase(CountryCode.NP, "NPL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NR
        [TestCase(CountryCode.NR, "NRU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.NU
        [TestCase(CountryCode.NU, "NIU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CK
        [TestCase(CountryCode.CK, "COK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.XK
        [TestCase(CountryCode.XK, "XKX")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CI
        [TestCase(CountryCode.CI, "CIV")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CH
        [TestCase(CountryCode.CH, "CHE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CO
        [TestCase(CountryCode.CO, "COL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CN
        [TestCase(CountryCode.CN, "CHN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CM
        [TestCase(CountryCode.CM, "CMR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CL
        [TestCase(CountryCode.CL, "CHL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CC
        [TestCase(CountryCode.CC, "CCK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CA
        [TestCase(CountryCode.CA, "CAN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CG
        [TestCase(CountryCode.CG, "COG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CF
        [TestCase(CountryCode.CF, "CAF")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CD
        [TestCase(CountryCode.CD, "COD")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CZ
        [TestCase(CountryCode.CZ, "CZE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CY
        [TestCase(CountryCode.CY, "CYP")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CX
        [TestCase(CountryCode.CX, "CXR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CR
        [TestCase(CountryCode.CR, "CRI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CW
        [TestCase(CountryCode.CW, "CUW")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CV
        [TestCase(CountryCode.CV, "CPV")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.CU
        [TestCase(CountryCode.CU, "CUB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SZ
        [TestCase(CountryCode.SZ, "SWZ")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SY
        [TestCase(CountryCode.SY, "SYR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SX
        [TestCase(CountryCode.SX, "SXM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KG
        [TestCase(CountryCode.KG, "KGZ")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KE
        [TestCase(CountryCode.KE, "KEN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SR
        [TestCase(CountryCode.SR, "SUR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KI
        [TestCase(CountryCode.KI, "KIR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KH
        [TestCase(CountryCode.KH, "KHM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KN
        [TestCase(CountryCode.KN, "KNA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KM
        [TestCase(CountryCode.KM, "COM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ST
        [TestCase(CountryCode.ST, "STP")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SK
        [TestCase(CountryCode.SK, "SVK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KR
        [TestCase(CountryCode.KR, "KOR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SI
        [TestCase(CountryCode.SI, "SVN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KP
        [TestCase(CountryCode.KP, "PRK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KW
        [TestCase(CountryCode.KW, "KWT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SN
        [TestCase(CountryCode.SN, "SEN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SM
        [TestCase(CountryCode.SM, "SMR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SL
        [TestCase(CountryCode.SL, "SLE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SC
        [TestCase(CountryCode.SC, "SYC")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KZ
        [TestCase(CountryCode.KZ, "KAZ")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.KY
        [TestCase(CountryCode.KY, "CYM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SG
        [TestCase(CountryCode.SG, "SGP")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SE
        [TestCase(CountryCode.SE, "SWE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.SD
        [TestCase(CountryCode.SD, "SDN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.DO
        [TestCase(CountryCode.DO, "DOM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.DM
        [TestCase(CountryCode.DM, "DMA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.DJ
        [TestCase(CountryCode.DJ, "DJI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.DK
        [TestCase(CountryCode.DK, "DNK")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.VG
        [TestCase(CountryCode.VG, "VGB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.DE
        [TestCase(CountryCode.DE, "DEU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.YE
        [TestCase(CountryCode.YE, "YEM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.DZ
        [TestCase(CountryCode.DZ, "DZA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.US
        [TestCase(CountryCode.US, "USA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.UY
        [TestCase(CountryCode.UY, "URY")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.YT
        [TestCase(CountryCode.YT, "MYT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.UM
        [TestCase(CountryCode.UM, "UMI")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LB
        [TestCase(CountryCode.LB, "LBN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LC
        [TestCase(CountryCode.LC, "LCA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LA
        [TestCase(CountryCode.LA, "LAO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TV
        [TestCase(CountryCode.TV, "TUV")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TW
        [TestCase(CountryCode.TW, "TWN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TT
        [TestCase(CountryCode.TT, "TTO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TR
        [TestCase(CountryCode.TR, "TUR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LK
        [TestCase(CountryCode.LK, "LKA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LI
        [TestCase(CountryCode.LI, "LIE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LV
        [TestCase(CountryCode.LV, "LVA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TO
        [TestCase(CountryCode.TO, "TON")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LT
        [TestCase(CountryCode.LT, "LTU")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LU
        [TestCase(CountryCode.LU, "LUX")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LR
        [TestCase(CountryCode.LR, "LBR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LS
        [TestCase(CountryCode.LS, "LSO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TH
        [TestCase(CountryCode.TH, "THA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TF
        [TestCase(CountryCode.TF, "ATF")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TG
        [TestCase(CountryCode.TG, "TGO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TD
        [TestCase(CountryCode.TD, "TCD")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.TC
        [TestCase(CountryCode.TC, "TCA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.LY
        [TestCase(CountryCode.LY, "LBY")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.VA
        [TestCase(CountryCode.VA, "VAT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.VC
        [TestCase(CountryCode.VC, "VCT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AE
        [TestCase(CountryCode.AE, "ARE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AD
        [TestCase(CountryCode.AD, "AND")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AG
        [TestCase(CountryCode.AG, "ATG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AF
        [TestCase(CountryCode.AF, "AFG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AI
        [TestCase(CountryCode.AI, "AIA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.VI
        [TestCase(CountryCode.VI, "VIR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IS
        [TestCase(CountryCode.IS, "ISL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IR
        [TestCase(CountryCode.IR, "IRN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AM
        [TestCase(CountryCode.AM, "ARM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AL
        [TestCase(CountryCode.AL, "ALB")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AO
        [TestCase(CountryCode.AO, "AGO")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AQ
        [TestCase(CountryCode.AQ, "ATA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AS
        [TestCase(CountryCode.AS, "ASM")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AR
        [TestCase(CountryCode.AR, "ARG")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AU
        [TestCase(CountryCode.AU, "AUS")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AT
        [TestCase(CountryCode.AT, "AUT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AW
        [TestCase(CountryCode.AW, "ABW")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IN
        [TestCase(CountryCode.IN, "IND")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AX
        [TestCase(CountryCode.AX, "ALA")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.AZ
        [TestCase(CountryCode.AZ, "AZE")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.IE
        [TestCase(CountryCode.IE, "IRL")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.ID
        [TestCase(CountryCode.ID, "IDN")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.UA
        [TestCase(CountryCode.UA, "UKR")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.QA
        [TestCase(CountryCode.QA, "QAT")]
        // CF: CountryExtensions.GetAcronym | switch (country) | case CountryCode.MZ
        [TestCase(CountryCode.MZ, "MOZ")]
        public void TestKnownCountryReturnsIsoAlpha3(CountryCode country, string expected)
            => Assert.That(country.GetAcronym(), Is.EqualTo(expected));

        // CF: CountryExtensions.GetAcronym | switch (country) | default
        [TestCase(CountryCode.Unknown, "Unknown")]
        [TestCase(CountryCode.A1, "A1")]
        public void TestUnmappedCountryFallsBackToEnumName(CountryCode country, string expected)
            => Assert.That(country.GetAcronym(), Is.EqualTo(expected));
    }
}
