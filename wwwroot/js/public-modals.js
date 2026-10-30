/* ===== NAU PUBLIC SITE-WIDE MODALS JAVASCRIPT =====
   Manages all shared modals (Academic Calendar, Results, Examination Schedule,
   Downloads, Eligibility Criteria, Required Documents, Professional Courses,
   Events & Activities) across all pages of the NAU website. */

(function () {
    'use strict';

    /* ===== ACADEMIC CALENDAR DATA ===== */
    let calendarDataLoaded = false;
    let calendarDataFetching = null;

    let calendarData = {
        resultDates: [
            { label: "ATC Refresher (Officers) No.29 — Result Announced", date: "12.01.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.30 — Result Announced", date: "06.04.2026", type: "result" },
            { label: "Audit Technique Course No.04 — Result Announced", date: "13.01.2026", type: "result" },
            { label: "Aeronautical Study Safety Assessment No.03 (Jan) — Result Announced", date: "20.01.2026", type: "result" },
            { label: "Aviation Economics Course No.03 — Result Announced", date: "27.01.2026", type: "result" },
            { label: "INDRA MSSR O&M No.05 — Result Announced", date: "03.02.2026", type: "result" },
            { label: "Supply & Demand in Air Transport Industry No.02 — Result Announced", date: "17.02.2026", type: "result" },
            { label: "Basic ATSEP Refresher Officers No.06 — Result Announced", date: "03.03.2026", type: "result" },
            { label: "Approach Control Surveillance No.06 — Result Announced", date: "20.03.2026", type: "result" },
            { label: "Air Passenger Rights No.01 — Result Announced", date: "09.04.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.31 — Result Announced", date: "04.06.2026", type: "result" },
            { label: "Area/Approach Control Surveillance No.60 — Mid-Term Result", date: "01.06.2026", type: "midterm" },
            { label: "Aerodrome Certification Course No.02 — Result Announced", date: "16.06.2026", type: "result" },
            { label: "Flight Procedure Design (Conventional) No.03 — Result Announced", date: "07.07.2026", type: "result" },
            { label: "Area/Approach Control Surveillance No.60 — Final Result Announced", date: "10.07.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.32 — Result Announced", date: "14.07.2026", type: "result" },
            { label: "Airport Economics Course No.01 — Result Announced", date: "21.07.2026", type: "result" },
            { label: "ANS Inspector Course No.01 — Result Announced", date: "04.08.2026", type: "result" },
            { label: "ELP Course No.05 — Mid-Term Result", date: "24.08.2026", type: "midterm" },
            { label: "ATC Refresher (Officers) No.33 — Result Announced", date: "11.08.2026", type: "result" },
            { label: "Aeronautical Mobile Service (Staff) No.05 — Result Announced", date: "08.09.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.34 — Result Announced", date: "08.09.2026", type: "result" },
            { label: "Aircraft Accident & Investigation Awareness No.01 — Result Announced", date: "08.09.2026", type: "result" },
            { label: "ATSEP Radar Technicians No.03 — Mid-Term Result", date: "14.09.2026", type: "midterm" },
            { label: "Facilitation (ICAO Annex-9) No.01 — Result Announced", date: "22.09.2026", type: "result" },
            { label: "Flight Procedure Design (PBN) No.01 — Result Announced", date: "03.11.2026", type: "result" },
            { label: "ATSEP Radar Technicians No.03 — Final Result Announced", date: "06.10.2026", type: "result" },
            { label: "Aviation Climate Change Course No.01 — Result Announced", date: "13.10.2026", type: "result" },
            { label: "Disabled Aircraft Recovery Course No.01 — Result Announced", date: "27.10.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.35 — Result Announced", date: "22.09.2026", type: "result" },
            { label: "Area/Approach Control Surveillance No.61 — Final Result Announced", date: "20.10.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.36 — Result Announced", date: "13.10.2026", type: "result" },
            { label: "Annex 14 Application and Requirements No.03 — Result Announced", date: "24.11.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.37 — Result Announced", date: "24.11.2026", type: "result" },
            { label: "SAR Coordinator Course No.02 — Result Announced", date: "15.12.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.38 — Result Announced", date: "08.12.2026", type: "result" },
            { label: "ATC Refresher (Officers) No.39 — Result Announced", date: "22.12.2026", type: "result" },
            { label: "Understanding ICAO Annex-19 Safety Mgmt No.03 — Result Announced", date: "15.12.2026", type: "result" },
            { label: "Basic ATC Course No.46 — Mid-Term Result (Estimated)", date: "08.03.2027", type: "midterm" }
        ],
        schools: [
            {
                name: "School of Air Navigation Services (ANS)",
                icon: "fa-tower-cell",
                color: "#0b2c4d",
                courses: [
                    { title: "Basic ATC Course No. 46", duration: "40 Weeks", start: "07.12.2026", end: "03.09.2027" },
                    { title: "ATC Refresher (Abridged) For Officers — No.29", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.30", duration: "01 Week", start: "30.03.2026", end: "03.04.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.31", duration: "01 Week", start: "25.05.2026", end: "29.05.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.32", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.33", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.34", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.35", duration: "01 Week", start: "14.09.2026", end: "19.09.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.36", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.37", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.38", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.39", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                    { title: "ATC Refresher (Abridged) For Officers — No.40", duration: "01 Week", start: "28.12.2026", end: "01.01.2027" },
                    { title: "ATC Assistant Course No. 03", duration: "08 Weeks", start: "07.09.2026", end: "30.10.2026" },
                    { title: "ATC Refresher (Abridged) For Staff — No.09", duration: "01 Week", start: "26.01.2026", end: "30.01.2026" },
                    { title: "ATC Refresher (Abridged) For Staff — No.10", duration: "01 Week", start: "18.05.2026", end: "22.05.2026" },
                    { title: "ATC Refresher (Abridged) For Staff — No.11", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                    { title: "ATC Refresher (Abridged) For Staff — No.12", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                    { title: "Approach Control Surveillance Course No. 06", duration: "05 Weeks", start: "09.02.2026", end: "13.03.2026" },
                    { title: "Area / Approach Control Surveillance Course No. 60", duration: "12 Weeks", start: "13.04.2026", end: "03.07.2026" },
                    { title: "Area / Approach Control Surveillance Course No. 61", duration: "12 Weeks", start: "27.07.2026", end: "16.10.2026" },
                    { title: "Flight Procedure Design (Conventional) Course No. 03", duration: "04 Weeks", start: "08.06.2026", end: "03.07.2026" },
                    { title: "Flight Procedure Design (PBN) Course No. 01", duration: "03 Weeks", start: "12.10.2026", end: "30.10.2026" },
                    { title: "ELP (English Language Proficiency) Advance Course No. 02", duration: "02 Weeks", start: "06.04.2026", end: "17.04.2026" },
                    { title: "ELP (English Language Proficiency) Course No. 05", duration: "03 Weeks", start: "10.08.2026", end: "04.09.2026" },
                    { title: "Basic SAR Course No. 22", duration: "02 Weeks", start: "02.03.2026", end: "13.03.2026" },
                    { title: "Basic SAR Course No. 23", duration: "02 Weeks", start: "31.08.2026", end: "11.09.2026" },
                    { title: "Basic SAR Course No. 24", duration: "02 Weeks", start: "16.11.2026", end: "27.11.2026" },
                    { title: "SAR Coordinator Course No. 02", duration: "02 Weeks", start: "30.11.2026", end: "11.12.2026" },
                    { title: "On Job Training (OJT) Course No. 01", duration: "01 Week", start: "12.01.2026", end: "16.01.2026" },
                    { title: "On Job Training (OJT) Course No. 02", duration: "01 Week", start: "11.05.2026", end: "15.05.2026" },
                    { title: "On Job Training (OJT) Course No. 03", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                    { title: "Managing Aeronautical Information Course No. 01", duration: "01 Week", start: "24.08.2026", end: "28.08.2026" },
                    { title: "Managing Aeronautical Information Course No. 02", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                    { title: "CAVRN (For Officers) Course No. 03", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                    { title: "CAVRN (For Officers) Course No. 04", duration: "01 Week", start: "01.06.2026", end: "05.06.2026" },
                    { title: "SWIM Course No. 02", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                    { title: "SWIM Course No. 03", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                    { title: "Aeronautical Radio Freq. Spectrum Mgmt (Officers) No. 04", duration: "01 Week", start: "09.02.2026", end: "13.02.2026" },
                    { title: "Aeronautical Radio Freq. Spectrum Mgmt (Officers) No. 05", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                    { title: "COO Course No. 09", duration: "14 Weeks", start: "TBD", end: "TBD" },
                    { title: "Communication Operation Assistant Course No. 21", duration: "10 Weeks", start: "TBD", end: "TBD" },
                    { title: "IDS AMHS Advance (For Officers) Course No. 04", duration: "02 Weeks", start: "27.07.2026", end: "07.08.2026" },
                    { title: "IDS AirNav AMHS (CRONOS) DB (Staff) Course No. 08", duration: "02 Weeks", start: "05.10.2026", end: "16.10.2026" },
                    { title: "IDS AirNav AMHS (CRONOS) DB (Staff) Course No. 09", duration: "02 Weeks", start: "07.12.2026", end: "18.12.2026" },
                    { title: "SDCPS Software (IOU) (Staff) Course No. 10", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                    { title: "SDCPS Software (IOU) (Staff) Course No. 11", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                    { title: "Aeronautical Mobile Service (Officers) Course No. 04", duration: "04 Weeks", start: "27.04.2026", end: "22.05.2026" },
                    { title: "Aeronautical Mobile Service (Staff) Course No. 05", duration: "03 Weeks", start: "17.08.2026", end: "04.09.2026" },
                    { title: "INDRA MSSR Operation & Maintenance Course No. 05", duration: "04 Weeks", start: "05.01.2026", end: "30.01.2026" },
                    { title: "ADSB Operation & Maintenance Course No. 04", duration: "01 Week", start: "02.03.2026", end: "06.03.2026" },
                    { title: "Selex (ILS) Operation & Maintenance Course No. 03", duration: "04 Weeks", start: "30.03.2026", end: "24.04.2026" },
                    { title: "INDRA PSR Operation & Maintenance Course No. 03", duration: "04 Weeks", start: "06.04.2026", end: "30.04.2026" },
                    { title: "ATM (Air Traffic Management) System Course No. 03", duration: "02 Weeks", start: "04.05.2026", end: "15.05.2026" },
                    { title: "ELDS MSSR Operation & Maintenance Course No. 04", duration: "04 Weeks", start: "20.07.2026", end: "14.08.2026" },
                    { title: "Selex DVOR / DME Operation & Maintenance Course No. 04", duration: "04 Weeks", start: "23.11.2026", end: "18.12.2026" },
                    { title: "Basic ATSEP Refresher (For Officers) Course No. 06", duration: "01 Week", start: "23.02.2026", end: "27.02.2026" },
                    { title: "ATSEP Data Processing & Networking (Technicians) No. 03", duration: "08 Weeks", start: "08.06.2026", end: "31.07.2026" },
                    { title: "ATSEP (Radar) For Technicians Course No. 03", duration: "07 Weeks", start: "17.08.2026", end: "02.10.2026" },
                    { title: "ATSEP (NAVAIDS) For Technicians Course No. 02", duration: "06 Weeks", start: "14.09.2026", end: "23.10.2026" },
                    { title: "ATSEP (Radio Communication) For Technicians Course No. 03", duration: "06 Weeks", start: "05.10.2026", end: "13.11.2026" },
                    { title: "INDRA VCCS SDC-2000 O&M Course No. 01", duration: "01 Week", start: "TBD", end: "TBD" },
                    { title: "HARRIS LIBERTY STAR VCCS O&M Course No. 01", duration: "01 Week", start: "TBD", end: "TBD" }
                ]
            },
            {
                name: "School of Regulatory Services",
                icon: "fa-scale-balanced",
                color: "#1e5f74",
                courses: [
                    { title: "International Air Law Course No. 01", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                    { title: "Managing Compliance with ICAO SARPs Course No. 02", duration: "01 Week", start: "18.05.2026", end: "22.05.2026" },
                    { title: "Air Cargo Regulatory Framework & Compliance Course No. 02", duration: "01 Week", start: "01.06.2026", end: "05.06.2026" },
                    { title: "Annex 14 Application and Requirements Course No. 03", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                    { title: "Aerodrome Certification Course No. 02", duration: "02 Weeks", start: "01.06.2026", end: "12.06.2026" },
                    { title: "Aerodrome Manual Course No. 02", duration: "01 Week", start: "29.06.2026", end: "03.07.2026" },
                    { title: "Aviation Economics Course No. 03", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                    { title: "Supply & Demand in Air Transport Industry Course No. 02", duration: "01 Week", start: "08.02.2026", end: "13.02.2026" },
                    { title: "Airport Economics Course No. 01", duration: "02 Weeks", start: "06.07.2026", end: "17.07.2026" },
                    { title: "Airport Marketing Course No. 03", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" },
                    { title: "Audit Technique Course No. 04", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                    { title: "Audit Technique Course No. 05", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                    { title: "Aeronautical Study Safety Assessment Course No. 03 (Jan)", duration: "01 Week", start: "12.01.2026", end: "16.01.2026" },
                    { title: "Aeronautical Study Safety Assessment Course No. 03 (Sep)", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                    { title: "Understanding ICAO Annex-19 Safety Management Course No. 02", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                    { title: "Understanding ICAO Annex-19 Safety Management Course No. 03", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                    { title: "Fundamentals of Air Cargo Operations & Safety (Staff) No. 01", duration: "01 Week", start: "13.07.2026", end: "17.07.2026" },
                    { title: "ANS Inspector Course No. 01", duration: "02 Weeks", start: "20.07.2026", end: "31.07.2026" },
                    { title: "Rules of the Air for Aviation Safety Professionals (ICAO Annex 02) No. 01", duration: "02 Weeks", start: "03.08.2026", end: "07.08.2026" },
                    { title: "Aviation Climate Change Course No. 01", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                    { title: "Air Passenger Rights Course No. 01", duration: "01 Week", start: "30.03.2026", end: "04.04.2026" },
                    { title: "Aircraft Accident and Investigation Awareness (ICAO Annex-13) No. 01", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                    { title: "Facilitation (ICAO Annex-9) Course No. 01", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                    { title: "Disabled Aircraft Recovery Course No. 01", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" }
                ]
            },
            {
                name: "School of Airport Services",
                icon: "fa-fire-extinguisher",
                color: "#c0392b",
                courses: [
                    { title: "Senior Aerodrome Fire Fighter Course No. 57", duration: "05 Weeks", start: "05.01.2026", end: "06.02.2026" },
                    { title: "Senior Aerodrome Fire Fighter Course No. 58", duration: "05 Weeks", start: "17.08.2026", end: "18.09.2026" },
                    { title: "Senior Aerodrome Fire Fighter Course No. 59", duration: "05 Weeks", start: "26.10.2026", end: "27.11.2026" },
                    { title: "Aerodrome Fire Fighter Refresher Course No. 12", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                    { title: "Aerodrome Fire Fighter Refresher Course No. 13", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                    { title: "Aerodrome Fire Fighter Refresher Course No. 14", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                    { title: "SVD (Refresher) Course No. 10", duration: "01 Week", start: "20.04.2026", end: "24.04.2026" },
                    { title: "SVD (Refresher) Course No. 11", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                    { title: "SVD (Refresher) Course No. 12", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                    { title: "Fire Fighting (Corporate Sector) Course No. 01", duration: "01 Week", start: "26.01.2026", end: "30.01.2026" },
                    { title: "Fire Fighting (Corporate Sector) Course No. 02", duration: "01 Week", start: "15.06.2026", end: "19.06.2026" },
                    { title: "Fire Fighting (Corporate Sector) Course No. 03", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                    { title: "Fire Fighting (Corporate Sector) Course No. 04", duration: "01 Week", start: "31.08.2026", end: "04.09.2026" },
                    { title: "Fire Fighting (Corporate Sector) Course No. 05", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                    { title: "Fire Fighting (Corporate Sector) Course No. 06", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" },
                    { title: "Fire Prevention Course No. 69", duration: "03 Weeks", start: "23.02.2026", end: "13.03.2026" },
                    { title: "Fire Prevention Course No. 70", duration: "03 Weeks", start: "01.06.2026", end: "19.06.2026" },
                    { title: "Fire Prevention Course No. 71", duration: "03 Weeks", start: "29.06.2026", end: "17.07.2026" },
                    { title: "Breathing Apparatus Course No. 24", duration: "03 Weeks", start: "30.03.2026", end: "17.04.2026" },
                    { title: "Breathing Apparatus Course No. 25", duration: "03 Weeks", start: "27.04.2026", end: "15.05.2026" },
                    { title: "Breathing Apparatus Course No. 26", duration: "03 Weeks", start: "20.07.2026", end: "07.08.2026" },
                    { title: "Junior Fire Officer Course No. 15", duration: "05 Weeks", start: "28.09.2026", end: "30.10.2026" },
                    { title: "Fire Investigation (For Officers) Course No. 01", duration: "02 Weeks", start: "30.11.2026", end: "11.12.2026" },
                    { title: "Aircraft Familiarization (For Officers) Course No. 01", duration: "01 Week", start: "28.12.2026", end: "01.01.2027" },
                    { title: "Fire Officer (Refresher) Course No. 14", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                    { title: "Aerodrome Operations (For Officers) Course No. 06", duration: "04 Weeks", start: "19.01.2026", end: "13.02.2026" },
                    { title: "Facilitation & Hospitality Management (Officers) Course No. 03", duration: "02 Weeks", start: "14.09.2026", end: "25.09.2026" },
                    { title: "Facilitation & Hospitality Management (Staff) Course No. 07", duration: "02 Weeks", start: "10.03.2026", end: "30.03.2026" },
                    { title: "Facilitation & Hospitality Management (Officers) Course No. 08", duration: "02 Weeks", start: "02.11.2026", end: "13.11.2026" },
                    { title: "Airside Safety Awareness Course No. 07 (JIAP)", duration: "01 Week", start: "05.01.2026", end: "09.01.2026" },
                    { title: "Airside Safety Awareness Course No. 08 (IIAP)", duration: "01 Week", start: "19.01.2026", end: "23.01.2026" },
                    { title: "Airside Safety Awareness Course No. 09 (QIAP)", duration: "01 Week", start: "06.07.2026", end: "10.07.2026" },
                    { title: "Airside Safety Awareness Course No. 10 (BKIAP)", duration: "01 Week", start: "03.08.2026", end: "07.08.2026" },
                    { title: "Airside Safety Awareness Course No. 11 (FIAP)", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                    { title: "Airside Safety Awareness Course No. 12 (AIAP)", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                    { title: "Airside Safety Awareness Course No. 13 (MIAP)", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                    { title: "Marshaller Refresher Course No. 02", duration: "01 Week", start: "16.02.2026", end: "20.02.2026" },
                    { title: "Marshaller Refresher Course No. 03", duration: "01 Week", start: "20.07.2026", end: "27.07.2026" },
                    { title: "Standard & Recommended Practices Aerodromes (Annex-14) (Engineers) No. 03", duration: "02 Weeks", start: "05.01.2026", end: "16.01.2026" },
                    { title: "Airfield Pavement Maintenance & Surface Evaluation (Engineers) No. 02", duration: "01 Week", start: "06.04.2026", end: "10.04.2026" },
                    { title: "PBB Maintenance (STP) Course No. 08", duration: "02 Weeks", start: "02.02.2026", end: "13.02.2026" },
                    { title: "PBB Maintenance (STP) Course No. 09", duration: "02 Weeks", start: "02.11.2026", end: "13.11.2026" },
                    { title: "Underground Cable Fault Repairs (Technicians) Course No. 03", duration: "02 Weeks", start: "04.05.2026", end: "15.05.2026" },
                    { title: "Engineering Estimation (Staff) Course No. 02", duration: "02 Weeks", start: "08.06.2026", end: "19.06.2026" },
                    { title: "FIDIC Contract (Officers) Course No. 01", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" }
                ]
            },
            {
                name: "School of Aviation Management",
                icon: "fa-chart-line",
                color: "#1e8e3e",
                courses: [
                    { title: "Aviation Management Course No. 09", duration: "05 Weeks", start: "12.01.2026", end: "13.02.2026" },
                    { title: "Aviation Management Course No. 10", duration: "05 Weeks", start: "19.10.2026", end: "20.11.2026" },
                    { title: "Personal Effectiveness (Staff) Course No. 06", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                    { title: "Personal Effectiveness (Staff) Course No. 07", duration: "01 Week", start: "07.09.2026", end: "11.09.2026" },
                    { title: "Personal Effectiveness (Staff) Course No. 08", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                    { title: "Basic Logistics (Staff) Course No. 05", duration: "03 Weeks", start: "19.01.2026", end: "06.02.2026" },
                    { title: "Basic Logistics (Staff) Course No. 06", duration: "03 Weeks", start: "20.04.2026", end: "08.05.2026" },
                    { title: "Basic Logistics (Staff) Course No. 07", duration: "03 Weeks", start: "13.07.2026", end: "31.07.2026" },
                    { title: "Basic Logistics (Staff) Course No. 08", duration: "03 Weeks", start: "09.11.2026", end: "27.11.2026" },
                    { title: "Logistics & Supply Chain Operations (Officers) No. 01", duration: "03 Weeks", start: "18.05.2026", end: "05.06.2026" },
                    { title: "Logistics & Supply Chain Operations (Officers) No. 02", duration: "03 Weeks", start: "07.09.2026", end: "25.09.2026" },
                    { title: "Human Factor in Aviation (Officers) Course No. 03", duration: "01 Week", start: "04.05.2026", end: "08.05.2026" },
                    { title: "Human Factor in Aviation (Officers) Course No. 04", duration: "01 Week", start: "26.10.2026", end: "30.10.2026" },
                    { title: "Introduction to Aviation IT System (Staff) Course No. 05", duration: "01 Week", start: "22.06.2026", end: "26.06.2026" },
                    { title: "Introduction to Aviation IT System (Staff) Course No. 06", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" },
                    { title: "IT/Cyber Security, Audit & Compliance Mgmt (Officers) No. 01", duration: "01 Week", start: "14.09.2026", end: "18.09.2026" },
                    { title: "IT/Cyber Security, Audit & Compliance Mgmt (Officers) No. 02", duration: "01 Week", start: "07.12.2026", end: "11.12.2026" },
                    { title: "MS Excel Basic (Staff) Course No. 01", duration: "01 Week", start: "13.04.2026", end: "17.04.2026" },
                    { title: "MS Excel Basic (Staff) Course No. 02", duration: "01 Week", start: "19.10.2026", end: "23.10.2026" },
                    { title: "Introduction to ERP (Staff) Course No. 01", duration: "01 Week", start: "16.11.2026", end: "20.11.2026" },
                    { title: "Introduction to ERP (Staff) Course No. 02", duration: "01 Week", start: "14.12.2026", end: "18.12.2026" },
                    { title: "E-Procurement through E-PADS (Officers) Course No. 03", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                    { title: "E-Procurement through E-PADS (Officers) Course No. 04", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                    { title: "Basic Office Organization (Asst/Supervisor) Course No. 21", duration: "02 Weeks", start: "05.01.2026", end: "16.01.2026" },
                    { title: "Basic Office Organization (Asst/Supervisor) Course No. 22", duration: "02 Weeks", start: "31.08.2026", end: "11.09.2026" },
                    { title: "Legal Proficiency (Officers) Course No. 01", duration: "03 Weeks", start: "26.01.2026", end: "13.02.2026" },
                    { title: "Legal Proficiency (Staff) Course No. 02", duration: "02 Weeks", start: "17.08.2026", end: "28.08.2026" },
                    { title: "Legal Proficiency (Staff) Course No. 03", duration: "02 Weeks", start: "12.10.2026", end: "23.10.2026" },
                    { title: "ITC Refresher Course No. 02", duration: "03 Days", start: "04.02.2026", end: "06.02.2026" },
                    { title: "ITC Course No. 33", duration: "02 Weeks", start: "06.04.2026", end: "17.04.2026" },
                    { title: "ITC Course No. 34", duration: "02 Weeks", start: "12.10.2026", end: "23.10.2026" },
                    { title: "ETC (Examination Technique Course) No. 14", duration: "01 Week", start: "08.06.2026", end: "12.06.2026" },
                    { title: "ETC (Examination Technique Course) No. 15", duration: "01 Week", start: "17.08.2026", end: "21.08.2026" },
                    { title: "Basic Mgmt System Standards (ISO/SMS) Officers Course No. 02 (SQMS)", duration: "01 Week", start: "06.04.2026", end: "10.04.2026" },
                    { title: "Basic Mgmt System Standards (ISO/SMS) Officers Course No. 03 (SQMS)", duration: "01 Week", start: "30.11.2026", end: "04.12.2026" },
                    { title: "Basic Mgmt System Standards (ISO/SMS) Staff Course No. 03 (SQMS)", duration: "01 Week", start: "13.04.2026", end: "17.04.2026" },
                    { title: "Basic Mgmt System Standards (ISO/SMS) Staff Course No. 04 (SQMS)", duration: "01 Week", start: "26.10.2026", end: "30.10.2026" },
                    { title: "Finance For Non-Financial Managers (Officers) Course No. 02", duration: "01 Week", start: "21.09.2026", end: "25.09.2026" },
                    { title: "Finance For Non-Financial Managers (Officers) Course No. 03", duration: "01 Week", start: "02.11.2026", end: "06.11.2026" },
                    { title: "PPRA Rules (Officers) Course No. 02", duration: "01 Week", start: "27.07.2026", end: "31.07.2026" },
                    { title: "PPRA Rules (Officers) Course No. 03", duration: "01 Week", start: "05.10.2026", end: "09.10.2026" },
                    { title: "Superintendent Development Course No. 05", duration: "01 Week", start: "09.02.2026", end: "13.02.2026" },
                    { title: "Superintendent Development Course No. 06", duration: "01 Week", start: "23.11.2026", end: "27.11.2026" }
                ]
            }
        ]
    };

    let activeSchool = 0;
    let calSearchTerm = '';
    let calStatusFilter = 'all';
    let calActiveView = 'courses';

    function parseDMY(str) {
        if (!str || str === 'TBD') return null;
        const parts = str.split('.');
        if (parts.length !== 3) return null;
        const d = new Date(parts[2], parts[1] - 1, parts[0]);
        return isNaN(d) ? null : d;
    }

    function isUpcoming(course) {
        const c = (typeof course === 'string') ? { start: course, end: null } : course;
        if (!c || !c.start || c.start === 'TBD') return false;
        const endDate = parseDMY(c.end) || parseDMY(c.start);
        if (!endDate) return false;
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        return endDate >= today;
    }

    function scheduleMidnightRefresh(renderFn) {
        const now = new Date();
        const nextMidnight = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 0, 0, 5);
        setTimeout(function tick() {
            try { renderFn(); } catch (e) { }
            setInterval(function () { try { renderFn(); } catch (e) { } }, 24 * 60 * 60 * 1000);
        }, nextMidnight - now);
    }

    window.fetchAcademicCalendarData = function (forceRefresh) {
        if (calendarDataLoaded && !forceRefresh) {
            return Promise.resolve(calendarData);
        }
        if (calendarDataFetching && !forceRefresh) {
            return calendarDataFetching;
        }
        calendarDataFetching = fetch('/Courses/AcademicCalendarData')
            .then(res => res.json())
            .then(data => {
                if (data && data.schools && data.schools.length > 0) {
                    calendarData.schools = data.schools;
                }
                if (data && data.resultDates && data.resultDates.length > 0) {
                    calendarData.resultDates = data.resultDates;
                }
                calendarDataLoaded = true;
                calendarDataFetching = null;
                return calendarData;
            })
            .catch(err => {
                console.error('Failed to load academic calendar data:', err);
                calendarDataFetching = null;
                return calendarData;
            });
        return calendarDataFetching;
    };

    window.refreshCalendarData = function () {
        return window.fetchAcademicCalendarData(true).then(() => {
            if (typeof renderCalendar === 'function') {
                renderCalendar();
            }
            if (typeof renderResultDates === 'function') {
                renderResultDates();
            }
        });
    };

    window.openCalendar = function (e, view) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        calActiveView = view || 'courses';
        if (!window._calMidnightRefreshScheduled) {
            window._calMidnightRefreshScheduled = true;
            scheduleMidnightRefresh(function () { renderCalendar(); renderResultDates(); });
        }

        const cal = document.getElementById('calendarModal');
        if (cal) cal.classList.add('open');
        document.body.style.overflow = 'hidden';

        window.fetchAcademicCalendarData().then(() => {
            if (calActiveView === 'results') {
                setTimeout(() => {
                    const tabs = document.querySelectorAll('.cal-view-tab');
                    tabs.forEach(t => t.classList.remove('active'));
                    const resultTab = document.getElementById('calTabResults');
                    if (resultTab) resultTab.classList.add('active');
                    const coursesTab = document.getElementById('calTabCourses');
                    if (coursesTab) coursesTab.classList.remove('active');
                    const searchWrap = document.querySelector('.cal-search-wrap');
                    const legendStrip = document.getElementById('calLegendStrip');
                    if (searchWrap) searchWrap.style.display = 'none';
                    if (legendStrip) legendStrip.style.display = 'none';
                    const titleEl = document.getElementById('calMainSchoolTitle');
                    if (titleEl) titleEl.textContent = 'Result Announcement Dates';
                    renderResultDates();
                }, 50);
            } else {
                renderCalendar();
            }
        });
    };

    window.closeCalendar = function () {
        const cal = document.getElementById('calendarModal');
        if (cal) cal.classList.remove('open');
        document.body.style.overflow = '';
    };

    function renderCalendar() {
        const school = calendarData.schools[activeSchool];
        const list = document.getElementById('calCourseList');
        const titleEl = document.getElementById('calMainSchoolTitle');
        const schoolsEl = document.getElementById('calSidebarSchools');
        if (!list) return;

        if (schoolsEl) {
            const allSchoolsLabel = `<div class="cal-sf-label-schools" style="margin-top:4px">Schools</div>`;
            schoolsEl.innerHTML = allSchoolsLabel + calendarData.schools.map((s, i) => `
                <button class="cal-sidebar-school-btn${i === activeSchool ? ' active' : ''}" onclick="switchCalSchool(${i})">
                    <span class="cal-sidebar-school-dot" style="background:${s.color}"></span>
                    <span>${s.name.replace('School of ', '')}</span>
                </button>`).join('');
        }

        if (titleEl) titleEl.textContent = school.name.replace('School of ', '');

        function parseDate(str) {
            if (!str || str === 'TBD') return null;
            const p = str.split('.');
            if (p.length !== 3) return null;
            return new Date(parseInt(p[2]), parseInt(p[1]) - 1, parseInt(p[0]));
        }

        const filteredCourses = school.courses.filter(c => {
            const matchSearch = !calSearchTerm || c.title.toLowerCase().includes(calSearchTerm.toLowerCase());
            const isTBD = c.start === 'TBD';
            const upcoming = !isTBD && isUpcoming(c);
            const completed = !isTBD && !upcoming;
            const matchFilter = calStatusFilter === 'all'
                || (calStatusFilter === 'upcoming' && upcoming)
                || (calStatusFilter === 'completed' && completed)
                || (calStatusFilter === 'tbd' && isTBD);
            return matchSearch && matchFilter;
        });

        const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
        const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        const year = 2026;

        const startMap = {};
        const rangeMap = {};

        filteredCourses.forEach((c, ci) => {
            const sd = parseDate(c.start);
            const ed = parseDate(c.end);
            if (!sd) return;
            const key = `${sd.getFullYear()}-${sd.getMonth()}-${sd.getDate()}`;
            if (!startMap[key]) startMap[key] = [];
            startMap[key].push({ c, ci, color: school.color });
            if (ed && ed > sd) {
                const d = new Date(sd);
                d.setDate(d.getDate() + 1);
                while (d <= ed) {
                    const rk = `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
                    if (!rangeMap[rk]) rangeMap[rk] = [];
                    rangeMap[rk].push({ c, ci, color: school.color });
                    d.setDate(d.getDate() + 1);
                }
            }
        });

        const activeMonths = new Set();
        filteredCourses.forEach(c => {
            const sd = parseDate(c.start);
            if (sd && sd.getFullYear() === year) activeMonths.add(sd.getMonth());
            const ed = parseDate(c.end);
            if (ed && ed.getFullYear() === year) activeMonths.add(ed.getMonth());
        });

        if (filteredCourses.length === 0) {
            list.innerHTML = `<div class="cal-empty"><i class="fas fa-search"></i><p>No courses found${calSearchTerm ? ' matching "' + calSearchTerm + '"' : ''}</p></div>`;
            return;
        }

        const today = new Date();
        let html = '<div class="cal-grid-wrap">';

        for (let m = 0; m < 12; m++) {
            if (!activeMonths.has(m)) continue;
            const firstDay = new Date(year, m, 1);
            const lastDay = new Date(year, m + 1, 0);
            const startDow = firstDay.getDay();
            const totalDays = lastDay.getDate();

            let monthCount = 0;
            for (let d = 1; d <= totalDays; d++) {
                const k = `${year}-${m}-${d}`;
                if (startMap[k]) monthCount += startMap[k].length;
            }

            html += `<div class="cal-month-block">
                <div class="cal-month-header">
                    <div class="cal-month-name">${MONTHS[m]} <span>${year}</span></div>
                    <div class="cal-month-count">${monthCount} course${monthCount !== 1 ? 's' : ''} start</div>
                </div>
                <div class="cal-week-days">${DAYS.map(d => `<div class="cal-wd">${d}</div>`).join('')}</div>
                <div class="cal-days-grid">`;

            for (let i = 0; i < startDow; i++) html += `<div class="cal-day empty"></div>`;

            for (let d = 1; d <= totalDays; d++) {
                const k = `${year}-${m}-${d}`;
                const starts = startMap[k] || [];
                const inRange = rangeMap[k] || [];
                const isToday = today.getFullYear() === year && today.getMonth() === m && today.getDate() === d;
                const hasCourse = starts.length > 0;
                const inRangeOnly = !hasCourse && inRange.length > 0;

                let cls = 'cal-day';
                if (isToday) cls += ' today';
                if (hasCourse) cls += ' has-course';
                if (inRangeOnly) cls += ' in-range';

                let dotsHtml = '';
                if (starts.length === 1 || starts.length === 2) {
                    dotsHtml = `<div class="cal-day-dots">${starts.map(ev => `<span class="cal-day-dot" style="background:${school.color}"></span>`).join('')}</div>`;
                } else if (starts.length > 2) {
                    dotsHtml = `<div class="cal-day-dots"><span class="cal-day-dot-more" style="color:${school.color}">2+</span></div>`;
                } else if (inRange.length > 0) {
                    dotsHtml = `<div class="cal-day-dots"><span class="cal-day-dot range-dot" style="background:${school.color}44"></span></div>`;
                }

                const tooltipCourses = starts.length > 0 ? starts : inRange;
                const tooltipAttr = tooltipCourses.length > 0
                    ? `data-tooltip="${tooltipCourses.slice(0, 3).map(ev => ev.c.title.substring(0, 45) + '...').join('|')}"`
                    : '';

                html += `<div class="${cls}" ${tooltipAttr} onclick="showDayCourses(${year},${m},${d},'${k}')" title="${starts.map(ev => ev.c.title).join('\n')}">
                    <span class="cal-day-num">${d}</span>
                    ${dotsHtml}
                </div>`;
            }

            const totalCells = startDow + totalDays;
            const remainder = totalCells % 7;
            if (remainder > 0) {
                for (let i = remainder; i < 7; i++) html += `<div class="cal-day empty"></div>`;
            }

            html += `</div></div>`;
        }

        html += `<div id="calDayPopup" class="cal-day-popup" style="display:none">
            <div class="cal-day-popup-header">
                <span id="calDayPopupTitle"></span>
                <button onclick="document.getElementById('calDayPopup').style.display='none'"><i class="fas fa-times"></i></button>
            </div>
            <div id="calDayPopupList"></div>
        </div>`;

        html += '</div>';
        list.innerHTML = html;

        window._calFilteredCourses = filteredCourses;
        window._calSchoolColor = school.color;
        window._calStartMap = startMap;
        window._calRangeMap = rangeMap;
    }

    function renderResultDates() {
        const list = document.getElementById('calCourseList');
        if (!list) return;
        const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
        const SHORT_MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

        function parseDate(str) {
            if (!str || str === 'TBD') return null;
            const p = str.split('.');
            if (p.length !== 3) return null;
            return new Date(parseInt(p[2]), parseInt(p[1]) - 1, parseInt(p[0]));
        }

        const byMonth = {};
        calendarData.resultDates.forEach(r => {
            const d = parseDate(r.date);
            if (!d) return;
            const m = d.getMonth();
            if (!byMonth[m]) byMonth[m] = [];
            byMonth[m].push({ ...r, dateObj: d });
        });

        const today = new Date();
        let html = '<div class="cal-results-wrap">';
        html += `<div style="background:#f0f7ff;border:1.5px solid #bfdbfe;border-radius:12px;padding:14px 18px;margin-bottom:20px;display:flex;align-items:center;gap:12px;font-size:13px;color:#1e40af;">
            <i class="fas fa-info-circle" style="font-size:16px;flex-shrink:0;"></i>
            <span>Results are officially published via the <strong>Student Online Portal</strong>. Dates below are estimated announcement dates based on course end dates (typically 1–2 weeks after course completion). Contact <strong>AdID.Academics@paa.gov.pk</strong> for confirmed dates.</span>
        </div>`;

        const sortedMonths = Object.keys(byMonth).map(Number).sort((a, b) => a - b);
        sortedMonths.forEach(m => {
            const items = byMonth[m].sort((a, b) => a.dateObj - b.dateObj);
            html += `<div class="cal-results-month">
                <div class="cal-results-month-title"><i class="fas fa-calendar-alt" style="color:#1e8e3e;font-size:12px;"></i>${MONTHS[m]} 2026 <span style="font-size:11px;color:#9aa5b4;font-weight:600;margin-left:4px;">${items.length} announcement${items.length !== 1 ? 's' : ''}</span></div>`;
            items.forEach(r => {
                const d = r.dateObj;
                const isPast = d < today;
                const typeIcon = r.type === 'midterm' ? 'fa-clipboard-check' : 'fa-poll';
                html += `<div class="cal-result-item" style="${isPast ? 'opacity:0.7;' : ''}">
                    <div class="cal-result-date-badge">
                        <div class="rdb-day">${d.getDate()}</div>
                        <div>${SHORT_MONTHS[d.getMonth()]}</div>
                    </div>
                    <div class="cal-result-icon ${r.type}"><i class="fas ${typeIcon}"></i></div>
                    <div class="cal-result-label">${r.label}</div>
                    <span class="cal-result-type-tag ${r.type}">${r.type === 'midterm' ? 'Mid-Term' : 'Final'}</span>
                    ${isPast ? '<span style="font-size:10px;color:#9aa5b4;font-weight:600;">Past</span>' : '<span style="font-size:10px;color:#1e8e3e;font-weight:600;">Upcoming</span>'}
                </div>`;
            });
            html += `</div>`;
        });
        html += '</div>';
        list.innerHTML = html;
    }

    window.switchCalSchool = function (i) {
        activeSchool = i;
        renderCalendar();
    };

    window.setCalFilter = function (filter, btn) {
        calStatusFilter = filter;
        document.querySelectorAll('.cal-sf-btn').forEach(b => b.classList.remove('active'));
        if (btn) btn.classList.add('active');
        renderCalendar();
    };

    window.switchCalView = function (view, btn) {
        calActiveView = view;
        document.querySelectorAll('.cal-view-tab').forEach(b => b.classList.remove('active'));
        if (btn) btn.classList.add('active');
        const searchWrap = document.querySelector('.cal-search-wrap');
        const legendStrip = document.getElementById('calLegendStrip');
        const sidebarSchools = document.getElementById('calSidebarSchools');
        const titleEl = document.getElementById('calMainSchoolTitle');
        const searchInput = document.getElementById('calSearchInput');
        if (view === 'results') {
            if (searchWrap) searchWrap.style.display = 'none';
            if (legendStrip) legendStrip.style.display = 'none';
            if (sidebarSchools) sidebarSchools.style.opacity = '0.4';
            if (titleEl) titleEl.textContent = 'Result Announcement Dates';
            if (searchInput) searchInput.value = '';
            renderResultDates();
        } else {
            if (searchWrap) searchWrap.style.display = '';
            if (legendStrip) legendStrip.style.display = '';
            if (sidebarSchools) sidebarSchools.style.opacity = '1';
            calSearchTerm = '';
            if (searchInput) searchInput.value = '';
            renderCalendar();
        }
    };

    window.calSearch = function (val) {
        calSearchTerm = val;
        renderCalendar();
    };

    window.showDayCourses = function (year, month, day, key) {
        const starts = (window._calStartMap || {})[key] || [];
        const ranges = (window._calRangeMap || {})[key] || [];
        const all = [...starts, ...ranges.filter(r => !starts.find(s => (s.c.id && r.c.id && s.c.id === r.c.id) || s.c.title === r.c.title))];
        if (!all.length) return;
        const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        const titleEl = document.getElementById('calDayPopupTitle');
        if (titleEl) titleEl.textContent = `${day} ${MONTHS[month]} ${year}`;
        const color = window._calSchoolColor || '#1e8e3e';

        const today = new Date();
        const isSelectedDateToday = today.getFullYear() === year && today.getMonth() === month && today.getDate() === day;

        const listEl = document.getElementById('calDayPopupList');
        if (listEl) {
            listEl.innerHTML = all.map(ev => {
                const isTBD = ev.c.start === 'TBD';
                const isStart = starts.some(s => (s.c.id && ev.c.id && s.c.id === ev.c.id) || s.c.title === ev.c.title);
                const st = isTBD ? 'tbd' : (isUpcoming(ev.c) ? 'upcoming' : 'past');
                const stLabel = isTBD ? 'TBD' : (isUpcoming(ev.c) ? 'Upcoming' : 'Completed');

                let startTag = '<span style="color:#8a94a2;font-size:10px">In progress</span>';
                if (isStart) {
                    if (isSelectedDateToday) {
                        startTag = '<span style="color:#1e8e3e;font-size:10px;font-weight:700">▶ Starts today</span>';
                    } else {
                        startTag = '<span style="color:#1e8e3e;font-size:10px;font-weight:700">▶ Course start</span>';
                    }
                }

                return `<div class="cal-popup-item">
                    <div class="cal-popup-dot" style="background:${color}"></div>
                    <div class="cal-popup-info">
                        <div class="cal-popup-title">${ev.c.title}</div>
                        <div class="cal-popup-meta">
                            <span><i class="fas fa-hourglass-half"></i> ${ev.c.duration}</span>
                            <span class="cc-card-badge ${st}" style="font-size:9px;padding:2px 7px">${stLabel}</span>
                            ${startTag}
                        </div>
                    </div>
                </div>`;
            }).join('');
        }
        const popup = document.getElementById('calDayPopup');
        if (popup) popup.style.display = 'block';
    };

    /* ===== SHARED UTILITY MODAL FUNCTIONS ===== */
    window.closeUtilModal = function (id) {
        const el = document.getElementById(id);
        if (el) el.classList.remove('open');
        document.body.style.overflow = '';
    };

    window.openResults = function (e) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        const el = document.getElementById('resultsModal');
        if (el) el.classList.add('open');
        document.body.style.overflow = 'hidden';
    };

    window.openExamSchedule = function (e) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        const el = document.getElementById('examModal');
        if (el) el.classList.add('open');
        document.body.style.overflow = 'hidden';
    };

    window.switchExamTab = function (btn, tab) {
        document.querySelectorAll('.exam-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.exam-tab-content').forEach(t => t.classList.remove('active'));
        if (btn) btn.classList.add('active');
        const content = document.getElementById('examTab-' + tab);
        if (content) content.classList.add('active');
    };

    window.openDownloads = function (e) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        const el = document.getElementById('downloadsModal');
        if (el) el.classList.add('open');
        document.body.style.overflow = 'hidden';
    };

    window.filterDownloads = function (query) {
        const q = (query || '').toLowerCase();
        document.querySelectorAll('.dl-item').forEach(item => {
            const tags = (item.dataset.tags || '') + ' ' + (item.querySelector('.dl-item-name')?.textContent || '');
            item.classList.toggle('hidden', q && !tags.toLowerCase().includes(q));
        });
        document.querySelectorAll('.dl-category').forEach(cat => {
            const visible = cat.querySelectorAll('.dl-item:not(.hidden)').length;
            cat.style.display = visible ? '' : 'none';
        });
    };

    window.openEligibility = function (e) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        const el = document.getElementById('eligibilityModal');
        if (el) el.classList.add('open');
        document.body.style.overflow = 'hidden';
    };

    window.switchEligTab = function (btn, tab) {
        document.querySelectorAll('.elig-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.elig-tab-content').forEach(t => t.classList.remove('active'));
        if (btn) btn.classList.add('active');
        const content = document.getElementById('eligTab-' + tab);
        if (content) content.classList.add('active');
    };

    window.openRequiredDocs = function (e, tab) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        const el = document.getElementById('reqDocsModal');
        if (el) el.classList.add('open');
        document.body.style.overflow = 'hidden';
        if (tab) {
            document.querySelectorAll('.rdoc-tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.rdoc-tab-content').forEach(t => t.classList.remove('active'));
            const tabContent = document.getElementById('rdocTab-' + tab);
            if (tabContent) tabContent.classList.add('active');
        }
    };

    window.switchRdocTab = function (btn, tab) {
        document.querySelectorAll('.rdoc-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.rdoc-tab-content').forEach(t => t.classList.remove('active'));
        if (btn) btn.classList.add('active');
        const content = document.getElementById('rdocTab-' + tab);
        if (content) content.classList.add('active');
    };

    window.openProfCourses = function (e, tab) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        const el = document.getElementById('profCoursesModal');
        if (el) el.classList.add('open');
        document.body.style.overflow = 'hidden';
        const targetTab = tab || 'pcaa';
        document.querySelectorAll('.pc-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.pc-tab-content').forEach(t => t.classList.remove('active'));
        const tabBtn = document.getElementById('pcTab-' + targetTab + '-btn');
        if (tabBtn) tabBtn.classList.add('active');
        const tabContent = document.getElementById('pcTab-' + targetTab);
        if (tabContent) tabContent.classList.add('active');
    };

    window.switchPcTab = function (btn, tab) {
        document.querySelectorAll('.pc-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.pc-tab-content').forEach(t => t.classList.remove('active'));
        if (btn) btn.classList.add('active');
        const content = document.getElementById('pcTab-' + tab);
        if (content) content.classList.add('active');
    };

    window.openUtilModal = function (id) {
        if (id === 'eventsModal') {
            window.openEvents();
        } else {
            const el = document.getElementById(id);
            if (el) el.classList.add('open');
            document.body.style.overflow = 'hidden';
        }
    };

    window.closeUtilModal = function (id) {
        const el = document.getElementById(id);
        if (el) el.classList.remove('open');
        document.body.style.overflow = '';
    };

    window.openEvents = function (e) {
        if (e && typeof e.preventDefault === 'function') e.preventDefault();
        const el = document.getElementById('eventsModal');
        if (el) el.classList.add('open');
        document.body.style.overflow = 'hidden';
    };

    window.switchEvTab = function (btn, tab) {
        document.querySelectorAll('.ev-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.ev-tab-content').forEach(t => t.classList.remove('active'));
        if (btn) btn.classList.add('active');
        const content = document.getElementById('evTab-' + tab);
        if (content) content.classList.add('active');
    };

    /* ===== KEYBOARD AND NAVIGATION EVENT LISTENERS ===== */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            ['resultsModal', 'calendarModal', 'examModal', 'downloadsModal',
             'eligibilityModal', 'reqDocsModal', 'profCoursesModal', 'eventsModal'].forEach(function (id) {
                const el = document.getElementById(id);
                if (el && el.classList.contains('open')) {
                    if (id === 'calendarModal') {
                        window.closeCalendar();
                    } else {
                        window.closeUtilModal(id);
                    }
                }
            });
            const calPopup = document.getElementById('calDayPopup');
            if (calPopup) calPopup.style.display = 'none';
        }
    });

    window.addEventListener('pageshow', function () {
        ['calendarModal', 'resultsModal', 'examModal', 'downloadsModal',
         'eligibilityModal', 'reqDocsModal', 'profCoursesModal', 'eventsModal'].forEach(function (id) {
            const el = document.getElementById(id);
            if (el) el.classList.remove('open');
        });
        document.body.style.overflow = '';
    });

    /* ===== NAU SPLASH SCREEN ANNOUNCEMENT POPUP ===== */
    var activeSplashId = null;

    window.closeNauSplash = function () {
        var modal = document.getElementById('nauSplashModal');
        if (modal) {
            modal.classList.remove('open');
            document.body.style.overflow = '';
        }
        if (activeSplashId) {
            sessionStorage.setItem('nau_splash_dismissed_' + activeSplashId, 'true');
        }
    };

    window.toggleSplashRemember = function (checked) {
        if (!activeSplashId) return;
        var today = new Date().toISOString().slice(0, 10);
        if (checked) {
            localStorage.setItem('nau_splash_dismissed_date_' + activeSplashId, today);
        } else {
            localStorage.removeItem('nau_splash_dismissed_date_' + activeSplashId);
        }
    };

    window.openSplashScreenPreview = function (data) {
        if (!data) return;
        renderSplashData(data);
        var modal = document.getElementById('nauSplashModal');
        if (modal) {
            modal.classList.add('open');
            document.body.style.overflow = 'hidden';
        }
    };

    function renderSplashData(data) {
        activeSplashId = data.newsPostId;
        var titleEl = document.getElementById('nauSplashTitle');
        var bodyEl = document.getElementById('nauSplashBody');
        var dateEl = document.getElementById('nauSplashDate');
        var catEl = document.getElementById('nauSplashCategory');
        var imgEl = document.getElementById('nauSplashImage');
        var heroEl = document.getElementById('nauSplashFallbackHero');
        var heroIconEl = document.getElementById('nauSplashHeroIcon');
        var readBtn = document.getElementById('nauSplashReadMoreBtn');

        if (titleEl) titleEl.textContent = data.title || 'Important Announcement';
        if (bodyEl) {
            var bodyText = data.body || '';
            if (bodyText.length > 320) {
                bodyText = bodyText.substring(0, 317) + '...';
            }
            bodyEl.textContent = bodyText;
        }
        if (dateEl) dateEl.innerHTML = '<i class="far fa-calendar-alt"></i> ' + (data.publishDate || 'Latest');
        if (catEl) {
            catEl.innerHTML = '<i class="' + (data.icon || 'fas fa-bullhorn') + '"></i> ' + (data.category || 'Announcement');
            if (data.color) catEl.style.background = data.color;
        }

        if (data.imageUrl && imgEl && heroEl) {
            imgEl.src = data.imageUrl;
            imgEl.style.display = 'block';
            heroEl.style.display = 'none';
        } else if (imgEl && heroEl) {
            imgEl.style.display = 'none';
            heroEl.style.display = 'flex';
            if (heroIconEl) heroIconEl.innerHTML = '<i class="' + (data.icon || 'fas fa-bullhorn') + '"></i>';
            if (data.color) heroEl.style.background = 'linear-gradient(135deg, #0b2c4d 0%, ' + data.color + ' 100%)';
        }

        if (readBtn) {
            readBtn.href = '/NewsPosts/Public';
            readBtn.onclick = function () {
                window.closeNauSplash();
            };
        }
    }

    async function initNauSplash() {
        var modal = document.getElementById('nauSplashModal');
        if (!modal) return;

        // Skip splash screen in admin edit modes (?edit=true)
        if (window.location.search.indexOf('edit=') !== -1) return;

        try {
            var res = await fetch('/NewsPosts/GetActiveSplashNews');
            if (!res.ok) return;
            var data = await res.json();
            if (!data || !data.hasSplash || !data.newsPostId) return;

            // Check if dismissed in this session
            if (sessionStorage.getItem('nau_splash_dismissed_' + data.newsPostId) === 'true') {
                return;
            }

            // Check if user selected "Don't show again today"
            var today = new Date().toISOString().slice(0, 10);
            if (localStorage.getItem('nau_splash_dismissed_date_' + data.newsPostId) === today) {
                return;
            }

            renderSplashData(data);

            // Smooth delay before showing splash screen
            setTimeout(function () {
                modal.classList.add('open');
                document.body.style.overflow = 'hidden';
            }, 750);
        } catch (e) {
            // Silently ignore if splash endpoint fails
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initNauSplash);
    } else {
        initNauSplash();
    }

    /* ===== HIDE APPLY ONLINE BUTTONS WHEN LOGGED IN ===== */
    function applyAuthVisibility() {
        var auth = window.NAU_AUTH || { isAuthenticated: false };
        if (auth.isAuthenticated) {
            document.documentElement.classList.add('nau-logged-in');
            if (document.body) document.body.classList.add('nau-logged-in');

            // Top-level nav links and footer links for Admissions redirect safely to CourseCatalog instead of Register
            document.querySelectorAll('a[href*="/Account/Register"], a[href="/Account/Register"]').forEach(function (el) {
                if (el.classList.contains('nav-link')) {
                    el.setAttribute('href', '/CourseCatalog');
                    return;
                }
                if (el.closest('.footer-col') && el.textContent.trim().toLowerCase() === 'admissions') {
                    el.setAttribute('href', '/CourseCatalog');
                    return;
                }
                el.style.display = 'none';
            });

            // Hide cards/buttons whose primary label is Apply Online or Apply Now
            document.querySelectorAll('a, button, span.feat-btn, div.util-info-row').forEach(function (el) {
                var text = el.textContent.trim().toLowerCase();
                if (text === 'apply online' || text === 'apply now' || text === 'apply online now →' || text === 'start application' || text === 'apply online now') {
                    var target = el.closest('.dd-featured') || el.closest('.util-info-row') || el;
                    target.style.display = 'none';
                }
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applyAuthVisibility);
    } else {
        applyAuthVisibility();
    }
    window.addEventListener('load', applyAuthVisibility);
    if (typeof MutationObserver !== 'undefined') {
        var authObs = new MutationObserver(applyAuthVisibility);
        if (document.body) {
            authObs.observe(document.body, { childList: true, subtree: true });
        } else {
            document.addEventListener('DOMContentLoaded', function () {
                if (document.body) authObs.observe(document.body, { childList: true, subtree: true });
            });
        }
    }
})();
