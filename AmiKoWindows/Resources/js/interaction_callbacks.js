function deleteRow(cmdID, currentRow) {
    try {
        if (cmdID == "DeleteAll") {
            window.external.JSNotify("deleteAllRows", 0);
        }
        else if (cmdID == "Interaktionen") {
            var table = document.getElementById(cmdID);
            var rowCount = table.rows.length;
            for (var i = 0; i < rowCount; i++) {
                var row = table.rows[i];
                if (row == currentRow.parentNode.parentNode) {
                    window.external.JSNotify("deleteSingleRow", row.cells[1].innerText);     // Call C#
                    table.deleteRow(i);		    					    // Delete row				
                    rowCount--; 					                    // Update counters
                    i--;
                }
            }
        }
    }
    catch (e) {
        // alert(e);
    }
}

function callEPhaAPI() {
    window.external.JSNotify("callEpha", 0);
}
