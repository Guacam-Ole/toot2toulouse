function fillServerdata() {
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
    }, function (error, whatever) {
        var e = error;
    });
    return false;
}

function switchAgreement() {
    var accepted = $("#agreement").prop('checked');
    $("#start").prop("disabled", !accepted);
}

function authMastodonStart() {
    var instance = $("#instance").val();
    $.getJSON("../mastodon/auth?instance=" + instance, function (data) {
        window.open(data);
    });

    $("#mastodoncode").show();
    $("#register").hide();
}

function codeEntered() {
    var code = $("#code").val();
    $("#finishmastodon").prop("disabled", code.length== 0);
}

function authMastodonFinish() {
    var instance = $("#instance").val();
    var code = $("#code").val();

    $.getJSON("../mastodon/code?instance=" + instance + "&code=" + code, function (data) {
        var success = data.key;

        if (success) {
            $("#mastodoncode").hide();
            $("#twitter").show();
        } else {
            $($("#error").find("p")).text(data.value);
            $("#error").show();
        }
    });
}



