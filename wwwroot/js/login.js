document.addEventListener("DOMContentLoaded", () => {
  const passwordInput = document.getElementById("inputPassword");
  const togglePasswordIcon = document.getElementById("toggle-password");

  togglePasswordIcon.addEventListener("click", () => {
    // Toggle the type attribute of the password input
    const type =
      passwordInput.getAttribute("type") === "password" ? "text" : "password";
    passwordInput.setAttribute("type", type);

    // Toggle the icon class
    togglePasswordIcon.classList.toggle("fa-eye");
    togglePasswordIcon.classList.toggle("fa-eye-slash");
  });
});
