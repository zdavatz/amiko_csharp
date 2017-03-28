function addShoppingCart(event) {
    // Do nothing for now
}

function displayFachinfo(ean, key, anchor) {
    try {
        if (anchor == 'undefined')
            anchor = '';
        window.external.JSNotify("displayFachinfo", ean);
    } 
    catch (e) {
        // alert(e);
    }
}