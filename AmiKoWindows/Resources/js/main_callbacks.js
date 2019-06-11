function addShoppingCart(event) {
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
        var elem = document.getElementById(anchor);
        if (elem !== null && typeof elem !== 'undefined') {
          var marks = elem.getElementsByClassName('mark');
          if (marks.length > 0) {
              marks[0].scrollIntoView(true);
          }
        }
    }
}


function highlightText(node, text) {
    if (node instanceof Text) {
        var splitted = node.data.split(text);
        if (splitted.length === 1) {
            // Not found, no replace
            return null;
        }
        // Create a new element with text highlighted
        var wrapper = document.createElement('span');
        for (var i = 0; i < splitted.length; i++) {
            var thisText = document.createTextNode(splitted[i]);
            wrapper.appendChild(thisText);
            if (i !== splitted.length - 1) {
                var thisSpan = document.createElement('span');
                thisSpan.innerText = text;
                thisSpan.style.backgroundColor = "yellow";
                wrapper.appendChild(thisSpan);
            }
        }
        return wrapper;
    }
    var nodeName = node.nodeName.toLowerCase();
    if (nodeName === 'script' || nodeName === 'style') {
        return null;
    }
    var nodes = node.childNodes;
    for (var i = 0; i < nodes.length; i++) {
        var thisNode = nodes[i];
        var newNode = highlightText(thisNode, text);
        if (newNode !== null) {
            node.replaceChild(newNode, thisNode);
        }
    }
    return null;
}
