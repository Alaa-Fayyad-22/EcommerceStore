document.addEventListener("DOMContentLoaded", function () {
    const buttons = document.querySelectorAll(".wishlist-btn");

    buttons.forEach(button => {
        button.addEventListener("click", function () {
            const productId = this.getAttribute("data-product-id");
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            const btn = this;

            fetch('/Wishlist/Toggle', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ productId: parseInt(productId) })
            })
                .then(response => {
                    if (!response.ok) throw new Error("Network response was not ok");
                    return response.json();
                })
                .then(data => {
                    if (data.success) {
                        if (data.action === "added") {
                            btn.classList.remove("btn-outline-danger");
                            btn.classList.add("btn-danger");
                            btn.innerHTML = "💔 Remove from Wishlist";
                        } else {
                            btn.classList.remove("btn-danger");
                            btn.classList.add("btn-outline-danger");
                            btn.innerHTML = "❤️ Add to Wishlist";
                        }
                    } else {
                        alert("Failed to update wishlist.");
                    }
                })
                .catch(error => console.error("Wishlist AJAX Error:", error));
        });
    });
});

