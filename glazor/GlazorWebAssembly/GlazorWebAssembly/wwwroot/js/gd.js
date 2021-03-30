//We exposed this function in order to easily access the glue42gd property
function ResolveValue(path, obj) {
    return path.split('.').reduce(function (prev, curr) {
        return prev ? prev[curr] : null
    }, obj || self)
}