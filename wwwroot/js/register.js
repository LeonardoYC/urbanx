// Esperar a que el contenido de la página se cargue
document.addEventListener("DOMContentLoaded", function () {
  // Obtener elementos de los íconos y campos de entrada
  const togglePassword = document.getElementById("toggle-password");
  const toggleConfirmPassword = document.getElementById(
    "toggle-confirm-password"
  );
  const passwordInput = document.getElementById("inputPassword");
  const confirmPasswordInput = document.getElementById("inputConfirmPassword");

  // Función para alternar el tipo de entrada entre texto y contraseña
  function togglePasswordVisibility(input, icon) {
    if (input.type === "password") {
      input.type = "text";
      icon.classList.add("fa-eye-slash");
      icon.classList.remove("fa-eye");
    } else {
      input.type = "password";
      icon.classList.add("fa-eye");
      icon.classList.remove("fa-eye-slash");
    }
  }

  // Agregar evento de clic al ícono de la contraseña
  togglePassword.addEventListener("click", function () {
    togglePasswordVisibility(passwordInput, togglePassword);
  });

  // Agregar evento de clic al ícono de confirmación de contraseña
  toggleConfirmPassword.addEventListener("click", function () {
    togglePasswordVisibility(confirmPasswordInput, toggleConfirmPassword);
  });
});
