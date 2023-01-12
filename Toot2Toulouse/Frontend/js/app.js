$(document).ready(function () {
    $.getJSON("../app/server", function (data) {
        var lastCategory = undefined;
        //var table = "";
        //$.each(data, function (key, val) {
        //    if (lastCategory != val.category) {
        //        if (lastCategory != undefined) {
        //            table += "</tbody></table>";
        //        }
        //        table += "<h4>" + val.category + "</h4>\n<table><thead><th>Setting</th><th class='right'>Value</th></thead><tbody>";
        //    }
        //    lastCategory = val.category;
        //    table += "<tr><td>" + val.displayName + "</td><td class='right'>";
        //    if (val.displayAsButton) {
        //        table += "<span class='fake button'>" + val.value + "</span>";
        //    } else {
        //        table += val.value;
        //    }
        //    table += "</td></tr>";
        //});

        var table = "<table><thead><th>Setting</th><th class=''>Value</th></thead><tbody>";
        $.each(data, function (key, val) {
            
            if (lastCategory != val.category) {
                //if (lastCategory != undefined) {
                    table += "<tr><td><strong>" + val.category + "</strong></td><td></td></tr>";
               // }
            }
            lastCategory = val.category;
            table += "<tr><td>" + val.displayName + "</td><td class=''>";
            if (val.displayAsButton) {
                table += "<span class='fake button'>" + val.value + "</span>";
            } else {
                table += val.value;
            }
            table += "</td></tr>";
        });
        table += "</tbody></table>";

        $(table).appendTo("#serverlimitstable");
    });

}); 

