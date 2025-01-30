import logging

class Logger:
    """
    Usage example:

        # Setting the log level for a specific category
        logger = Logger.get_global_logger(category="General", level=logging.DEBUG, log_to_file=True, log_filename="app.log")
        logger.debug("This debug message will be shown.")

        # Set the log level for "Module1" to only show warnings and errors
        Logger.set_log_level(category="Simulation", level=logging.WARNING)

        # Use filters to show logs of a certain level
        Logger.set_log_filter(category="General", level=logging.ERROR)

        # Only error messages from "General" category will be displayed
        general_logger = Logger.get_logger(category="General")
        general_logger.info("This info message will NOT be shown.")
        general_logger.error("This error message will be shown.")
            
    """
    def __init__(self):
        self.logger = None
        self._configure_default_logger()
        
    @classmethod
    def _configure_default_logger(cls, level=logging.INFO, log_to_file=False, log_filename=None):
        """
        Configure the default logger for the entire class.
        """
        logger = logging.getLogger("General")
        logger.setLevel(level)

        # Set up the formatter
        formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

        # Console handler
        console_handler = logging.StreamHandler()
        console_handler.setFormatter(formatter)
        logger.addHandler(console_handler)

        # If we want to log to a file
        if log_to_file and log_filename:
            file_handler = logging.FileHandler(log_filename)
            file_handler.setFormatter(formatter)
            logger.addHandler(file_handler)

    @classmethod
    def get_logger(cls, category="General", level=logging.INFO, log_to_file=False, log_filename=None):
        logger = logging.getLogger(category)
        logger.propagate = False  # Disable propagation
        logger.setLevel(level)

        if not logger.handlers:  # Add handlers only once
            formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
            console_handler = logging.StreamHandler()
            console_handler.setFormatter(formatter)
            logger.addHandler(console_handler)
            if log_to_file and log_filename:
                file_handler = logging.FileHandler(log_filename)
                file_handler.setFormatter(formatter)
                logger.addHandler(file_handler)
        return logger

    @classmethod
    def get_global_logger(cls, category="General", level=logging.INFO, log_to_file=False, log_filename=None):
        """
        Returns a global logger instance.
        """
        if not hasattr(cls, "_global_logger"):
            cls._global_logger = cls.get_logger(category, level, log_to_file, log_filename)
        return cls._global_logger

    @staticmethod
    def set_log_level(category="General", level=logging.INFO):
        """
        Set log level for a specific category globally.
        """
        logger = logging.getLogger(category)
        logger.setLevel(level)

    @staticmethod
    def set_log_filter(category="General", level=logging.INFO):
        """
        Add a filter to show only logs of a certain level or category.
        """
        logger = logging.getLogger(category)
        # Remove existing filters to avoid stacking them
        logger.filters = []
        
        # Create a new filter based on the level
        log_filter = logging.Filter()
        log_filter.filter = lambda record: record.levelno >= level
        logger.addFilter(log_filter)

