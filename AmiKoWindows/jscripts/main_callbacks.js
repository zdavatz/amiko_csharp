﻿function addShoppingCart(event) {
    // Do nothing for now
}

function displayFachinfo(ean, anchor) {
    try {
        if (anchor == 'undefined')
            anchor = '';
        window.external.JSNotify("displayFachinfo", ean, anchor);
    } 
    catch (e) {
        // alert(e);
    }
}

/**
 * Identifies the anchor's id and scrolls to the first mark tag.
 * Javascript is brilliant :-)
 * @param anchor
 */
function moveToHighlight(anchor) {
    if (typeof anchor !== 'undefined') {
        var id = document.getElementById(anchor);
        if (id !== null && id.hasAttribute('id')) {
            id = id.getElementsByTagName('mark');
            $(window).scrollTop($(id).offset().top - 120);
        }
    }
}