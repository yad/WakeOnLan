// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

setInterval(() => location.reload(), 5000);

const onClick = async (ev, label) => {
    ev.preventDefault();
    const action = ev.target.parentElement.getAttribute('action');
    console.log(action);
    let post = action.includes("mode=stop") ? confirm(`Stop '${label}' ?`) : true;

    if (post) {
        await fetch(action, {
            "headers": {
                "content-type": "application/x-www-form-urlencoded"
            },
            "method": "POST",
            "body": `__RequestVerificationToken=${document.getElementsByName("__RequestVerificationToken")[0].value}`,
        });
        location.reload();
    }
}
