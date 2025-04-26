export async function toClipboard(text) {
    console.log("hi");
    await navigator.clipboard.writeText(text);
}