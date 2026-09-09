window.onload = function () {
    // Data setup based on your physical ID card samples
    const traineeData = {
        // Front Side
        name: "LOVELY ROSE L. ANDAYA",
        idNumber: "2025-077",
        start: "January 26, 2026",
        valid: "February 26, 2026",
        issued: "January 26, 2026",
        photo: "assets/img/trainee-photo.jpg", // Path to your photo
        
        // Back Side (Dynamic School Info)
        schoolName: "COMMONWEALTH HIGH SCHOOL",
        schoolAddress: "ECOLS ST. BRGY COMMONWEALTH, QUEZON CITY",

        // Emergency Info
        emgName: "MARYROSE L. ANDAYA",
        emgAddress: "239 DON FABIAN ST. BRGY. COMMONWEALTH, QUEZON CITY",
        emgPhone: "0906 921 1834"
    };

    // Helper to push values into HTML
    const setUI = (id, val) => {
        const el = document.getElementById(id);
        if (el) {
            if (el.tagName === "IMG") el.src = val;
            else el.innerText = val;
        }
    };

    // Populate Front
    setUI("js-name", traineeData.name);
    setUI("js-idnum", traineeData.idNumber);
    setUI("js-start", traineeData.start);
    setUI("js-valid", traineeData.valid);
    setUI("js-issued", traineeData.issued);
    setUI("js-photo", traineeData.photo);
    
    // Generate QR (using the ID number)
    const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${traineeData.idNumber}`;
    setUI("js-qr", qrUrl);

    // Populate Back
    setUI("js-school-name", traineeData.schoolName);
    setUI("js-school-address", traineeData.schoolAddress);
    setUI("js-emg-name", traineeData.emgName);
    setUI("js-emg-address", traineeData.emgAddress);
    setUI("js-emg-phone", traineeData.emgPhone);
    
    console.log("ID Card System: All data loaded successfully.");
};