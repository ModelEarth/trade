
// enable type-hints
/// <reference types="../dist/useeio" />
/** @type {import('useeio')} */
var useeio = useeio;

/*
const model = useeio.modelOf({
  endpoint: 'http://localhost:8080/api',
  model: 'USEEIOv2.0',
  asJsonFiles: true,
});
*/

/*
// Public API Discontinued. Use asJsonFiles instead.
const model = useeio.modelOf({
  endpoint: 'https://smmtool.app.cloud.gov/api',
  model: 'USEEIOv2.0.1-411',
  asJsonFiles: false,
});
*/
let model = getModel();
function getModelFolderName() {
    let hash = getUrlHash();
    let theModel = "USEEIOv2.0.1-411";
    if (hash.state) { // Prior to 2024 states were GA, ME, MN, OR, WA
        let thestate = hash.state.split(",")[0].toUpperCase();
        theModel = thestate + "EEIOv1.0-s-20"
    }
    return theModel;
}
function getModel() {
    let theModel = getModelFolderName()
    return useeio.modelOf({ // Calls the getJson() method set in webapi.ts
      //endpoint: 'http://localhost:8887/useeio-json/models/2020',

      // So clone the useeio-json repo into the same webroot.
      endpoint: '/useeio-json/models/2020',

      // CORS error
      endpoint: 'https://model.earth/useeio-json/main/models/2020',
      endpoint: 'https://raw.githubusercontent.com/ModelEarth/useeio-json/main/models/2020',
      
      model: theModel,
      asJsonFiles: true,
    });
}

function formatCell(input, format) {
    // If format is none or blank, return input as it is.
    if (format === 'none' || format === '') {
        return input;
    }

    // Format as scientific notation
    if (format === 'scientific') {
        return input.toExponential(1);
    }

    // Format as easy
    if (input >= 1e12) {
        // Round to billions
        return (input / 1e12).toFixed(3) + ' Trillion';
    } else if (input >= 1e9) {
        // Round to billions
        return (input / 1e9).toFixed(1) + ' Billion';
    } else if (input >= 1e6) {
        // Round to millions
        return (input / 1e6).toFixed(1) + ' Million';
    } else if (input >= 1000) {
        // Round to thousands
        return (input / 1000).toFixed(1) + ' K';
    } else if (input >= 0) {
        // Round to one decimal. Remove .0
        return input.toFixed(1).replace(/\.0$/, '');
    } else if (input >= 0.0001) {
        // Round to one decimal
        return input.toFixed(4);
    } else if (input >= -1000) {
        return (input / 1e3).toFixed(1) + ' K';
    } else if (input >= -1e9) {
        // Round to -millions
        return (input / 1e6).toFixed(1).replace(/\.0$/, '') + ' Million';
    } else if (input >= -1e12) {
        // Round to -billions
        return (input / 1e9).toFixed(1).replace(/\.0$/, '') + ' Billion';
    } else {
        // Format with scientific notation with one digit after decimal
        return input.toExponential(1);
    }
}

// Test cases
//console.log(formatCell(42262000000, 'easy')); // Output: "42.3 Billion"
//console.log(formatCell(9500000, 'easy'));     // Output: "9.5 Million"
//console.log(formatCell(50000, 'easy'));       // Output: "50.0 K"
//console.log(formatCell(99.99, 'easy') + " - BUG, let's avoid adding .0 when rounding");        // Output: "100.0" - 
//console.log(formatCell(0.0005, 'easy'));      // Output: "5.0e-4"
// console.log(formatCell(45000000, 'scientific')); // Output: "4.5e+7"
// NOT USED - Will probably delete. Tabulator Intl.NumberFormat used instead.
function formatNum(numberString, locale = navigator.language) {
    if (typeof numberString !== 'string') {
        numberString = String(numberString);
    }
    // Remove existing formatNum or periods
    let cleanString = numberString.replace(/[,.]/g, '');
    
    // Check if the cleaned string is a valid number
    if (isNaN(cleanString)) {
        return numberString; // Return the original string if it's not a valid number
    }
    // Check if the locale is valid; default to 'en-US' if not
    if (!locale || typeof locale !== 'string' || !Intl.NumberFormat.supportedLocalesOf([locale]).length) {
        locale = 'en-US';
    }
    // Convert to a number
    let number = parseFloat(cleanString);
    
    // Format the number based on the locale
    return number.toLocaleString(locale);
}
/*
// US standard (default)
console.log(formatNum("1234567.89")); // Output: "1,234,567.89"

// German standard
console.log(formatNum("1234567,89", 'de-DE')); // Output: "1.234.567,89"

// French standard
console.log(formatNum("1234567.89", 'fr-FR')); // Output: "1 234 567,89"

// Invalid number string remains unchanged
console.log(formatNum("12a34567.89")); // Output: "12a34567.89"

// Number with existing formatNum/periods
console.log(formatNum("1,234,567.89", 'en-US')); // Output: "1,234,567.89"
console.log(formatNum("1.234.567,89", 'de-DE')); // Output: "1.234.567,89"
*/

function getUrlHash() {
  return (function (pairs) {
    if (pairs == "") return {};
    var result = {};
    pairs.forEach(function(pair) {
      // Split the pair on "=" to get key and value
      var keyValue = pair.split('=');
      var key = keyValue[0];
      var value = keyValue.slice(1).join('=');

      // Replace "%26" with "&" in the value
      value = value.replace(/%26/g, '&');

      // Set the key-value pair in the result object
      result[key] = value;
    });
    return result;
  })(window.location.hash.substr(1).split('&'));
}
