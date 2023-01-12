$(document).ready(function () {
    $.getJSON("../app/server", function (data) {
        var lastCategory = undefined;

        var table = "<table><tbody>";
        $.each(data, function (key, val) {
            if (lastCategory != val.category) {
                table += "<tr><td><strong>" + val.category + "</strong></td><td></td></tr>";
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