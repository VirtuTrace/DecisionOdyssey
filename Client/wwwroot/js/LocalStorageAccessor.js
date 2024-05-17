export function get(key)
{
    let value = window.localStorage.getItem(key);
    return value ? value : null;
}

export function exists(key)
{
    return window.localStorage.getItem(key) !== null;
}

export function set(key, value)
{
    window.localStorage.setItem(key, value);
}

export function clear()
{
    window.localStorage.clear();
}

export function remove(key)
{
    window.localStorage.removeItem(key);
}