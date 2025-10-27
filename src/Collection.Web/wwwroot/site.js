window.nc = window.nc || {};

window.nc.postLogin = async function (password) {
    const form = new FormData();
    form.append("Password", password || "");

    const resp = await fetch("/login.ajax", {
        method: "POST",
        body: form,
        credentials: "include" // make sure the browser stores/sends cookies
    });

    return { ok: resp.ok, status: resp.status };
};
