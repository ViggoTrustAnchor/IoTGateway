function NativeHeader() {
    const header = document.getElementById("native-header");
    Array.from(header.children[0].children[1].getElementsByTagName("ul")).forEach(subMenu => {
        const sibling = subMenu.nextElementSibling || subMenu.previousElementSibling;

        // if link dose not go anywhere, expand on click
        sibling.addEventListener("click", event => {
            // expand menue on mobile view
            if (window.matchMedia("screen and (max-width: 900px)").matches) {
                event.preventDefault(); // if link, prevent opening
                subMenu.toggleAttribute("expanded");
            }
        })

        subMenu.parentElement.addEventListener("mouseenter", () => {
            // if on computer, make sure dropdowns expand beyond the screen

            const rect = subMenu.getBoundingClientRect();
            const rightX = rect.left + rect.width

            // content is overflowing
            if (rightX > window.innerWidth) {
                // go to top submenu
                let element = subMenu
                while (element.parentElement.parentElement.parentElement.tagName.toLowerCase() == "li") {
                    element = element.parentElement.parentElement
                }

                let oldOffset = 0
                if (element.style.transform)
                    oldOffset += Number(element.style.transform.split("(")[1].split("px")[0])
                element.style.transform = `translateX(${oldOffset + window.innerWidth - rightX}px)`

                const topLevelLi = element.parentElement

                const handler = () => {
                    topLevelLi.removeEventListener("mouseenter", handler);
                    element.style.transform = ""
                };
                topLevelLi.addEventListener("mouseenter", handler);
            }
        })
    })



    function ToggleNav() {
        header.toggleAttribute("data-visible");
    }

    return {
        ToggleNav
    }
}

function Popup() {
    const popupStack = [];

    function PopStack(args) {
        popupStack.pop().OnPop(args)
        DisplayPopup()
    }

    function DisplayPopup() {
        if (popupStack.length) {
            document.getElementById("native-popup-container").innerHTML = popupStack[popupStack.length - 1].html
        }
        else {
            document.getElementById("native-popup-container").innerHTML = ""
        }
    }

    async function Popup(html) {
        return new Promise((resolve, reject) => {
            popupStack.push({
                html: html,
                OnPop: (args) => resolve(args),
            })
            DisplayPopup()
        })
    }
    async function Alert(message) {
        const html = CreateHTMLAlertPopup({ Message: `<p>${message}</p>`});
        await Popup(html);
    }
    function AlertOk() {
        PopStack()
    }

    async function Confirm(message) {
        const html = CreateHTMLConfirmPopup({ Message: `<p>${message}</p>`});
        return await Popup(html);
    }

    function ConfirmYes() {
        PopStack(true)
    }
    function ConfirmNo() {
        PopStack(false)
    }

    async function Prompt(message) {
        const html = CreateHTMLPromptPopup({ Message: `<p>${message}</p>`});
        return await Popup(html);
    }

    async function PromptSubmit(value) {
        PopStack(value)
    }

    return {
        Alert,
        AlertOk,
        Confirm,
        ConfirmYes,
        ConfirmNo,
        Prompt,
        PromptSubmit,
    }
}

let popup;
let nativeHeader;

async function Test() {
    while (true) {
        const input = await popup.Prompt("name a color")
        const confirm = await popup.Confirm(`is you favorite color is ${input}?`)

        if (confirm) {
            await popup.Alert("good choise")
            break
        } else {
            await popup.Alert("let´s try again?")
        }
    }
}

window.addEventListener("load", () => {
    nativeHeader = NativeHeader();
    popup = Popup()

    popup.Alert(`Exception in thread "main" java.lang.NullPointerException
        at com.example.myproject.Book.getTitle(Book.java:16)
        at com.example.myproject.Author.getBookTitles(Author.java:25)
        at com.example.myproject.Bootstrap.main(Bootstrap.java:14)`)
})

