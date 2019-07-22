const apiBase = "http://localhost:7071/api";

const workflows = [
    "UserConfirmationWorkflow",
    "UserMonitorWorkflow",
    "GameMonitorWorkflow",
    "RunUserParallelWorkflow",
    "RunUserSequentialWorkflow"];

const state = {
    requests: 0,
    inp: {},
    btn: {},
    div: {},
    username: '',
    active: false
};

state.inp.username = document.getElementById("username");
state.inp.action = document.getElementById("action");
state.inp.target = document.getElementById("target");
state.inp.with = document.getElementById("with");

state.btn.toggle = document.getElementById("toggle");
state.btn.new = document.getElementById("new");
state.btn.confirm = document.getElementById("confirm");
state.btn.action = document.getElementById("btnAction");

state.div.lastError = document.getElementById("lastError");
state.div.description = document.getElementById("description");

const toggleFn = () => {
    state.active = !state.active;
    state.btn.toggle.innerText = state.active ? "Pause" : "Run";
};

toggleFn();

state.btn.toggle.addEventListener("click", toggleFn);

const refreshUser = () => state.username = state.inp.username.value;
state.inp.username.addEventListener("keyup", refreshUser);

const post = (url, data) => {
    state.requests += 1;
    return fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
    });
};

const userActionDone = () => {
    state.requests -= 1;
    state.btn.new.disabled =
        state.inp.username.disabled =
        state.btn.confirm.disabled =
        state.btn.action.disabled = false;
};

const userAction = (action, data) => {
    state.btn.new.disabled =
        state.inp.username.disabled =
        state.btn.confirm.disabled =
        state.btn.action.disabled = true;
    post(`${apiBase}/${action}`, data || {
        'name': state.username
    }).then(resp => {
        userActionDone();
        if (!resp.ok) {
            resp.text().then(text => {
                state.div.lastError.innerText = `${action} failed: ${text}.`;
            });
        }
        else {
            state.div.lastError.innerText = '';
        }
    }, rejected => {
        userActionDone();
        state.div.lastError.innerText = rejected.message;
    });
}

const newUser = () => userAction("NewUser");
const confirmUser = () => userAction("ConfirmUser");

const doAction = () => {

    const data = {
        "name": state.username,
        "action": state.inp.action.selectedOptions[0].value,
        "target": state.inp.target.value,
        "with": state.inp.with.value
    };

    state.inp.target.value = '';
    state.inp.with.value = '';
    userAction("Action", data);
};

state.btn.new.addEventListener("click", newUser);
state.btn.confirm.addEventListener("click", confirmUser);
state.btn.action.addEventListener("click", doAction);

const monitorWorkflowStatus = (workflow) => {
    let div = document.getElementById(workflow);
    let uri = `${apiBase}/CheckStatus/${state.username}/${workflow}`;
    state.requests += 1;
    fetch(uri, {
        method: 'GET'
    })
        .then(resp => {
            state.requests -= 1;
            if (!resp.ok) {
                div.className = "status unknown";
            }
            else {
                resp.json().then(res => {
                    if (["Running", "Completed", "Failed"].indexOf(res.status) >= 0) {
                        div.className = `status ${res.status.toLowerCase()}`;
                    }
                    else {
                        div.className = "status unknown";
                    }
                });
            }
        }, () => {
            state.requests -= 1;
            div.className = "status unknown";
        });
};

const monitorWorkflows = () => {
    workflows.forEach(workflow => monitorWorkflowStatus(workflow));
};

const tag = (tagName, txt) => `<${tagName}>${txt}</${tagName}>`;

const statusUpdate = () => {
    state.requests += 1;
    fetch(`${apiBase}/GameStatus/${state.username}`)
        .then(res => {
            state.requests -= 1;
            if (!res.ok) {
                state.div.description.innerHTML = tag('h2', 'The Universe is Empty');
            }
            else {
                res.json().then(universe => {
                    if (!universe.user.isAlive) {
                        state.div.description.innerHTML =
                            tag('h2', `${state.username} is Dead`);
                        return;
                    }
                    if (!universe.user.currentRoom) {
                        state.div.description.innerHTML =
                            tag('h2', `The Universe is Waiting for ${state.username}`);
                        return;
                    }
                    let description = tag('h2', universe.room.name) +
                        tag('p', universe.room.description);

                    if (universe.user.inventoryItems) {
                        description += `<p>${state.username} is holding onto: ${universe.user.inventoryItems}.`;
                    }

                    if (universe.monster.isAlive) {
                        description +=
                            tag('p', `Inside the room is a ${universe.monster.name}.`);
                        if (universe.monster.inventoryItems) {
                            description += tag('p', `It is holding onto a ${universe.monster.inventoryItems}.`);
                        }
                    }
                    else {
                        description +=
                            tag('p', `There is a dead ${universe.monster.name} on the floor.`);
                    }

                    if (universe.room.inventoryItems) {
                        description +=
                            tag('p', `On the floor ${state.username} sees a ${universe.room.inventoryItems}.`);
                    }

                    if (universe.user.inventoryItems && universe.user.inventoryItems.split(',').length > 1) {
                        description +=
                            tag('p', tag('strong', `${state.username} won the game!`));
                    }

                    state.div.description.innerHTML =
                        description;

                });
            }
        }, () => {
            state.requests -= 1;
            state.div.description.innerHTML =
                tag('h2', 'The Universe is Chaos');
        });
};

const tick = () => {
    if (state.active && state.username.length > 1
        && state.requests <= 0) {
        monitorWorkflows();
        statusUpdate();
    }
    setTimeout(tick, 500);
};

setTimeout(() => {
    state.inp.username.focus();
    tick();
});